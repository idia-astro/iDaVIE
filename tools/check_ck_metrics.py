#!/usr/bin/env python3
"""
tools/check_ck_metrics.py

Regex-based CK metric computation for C# source files.

Metrics (per class):
  WMC  - Weighted Methods per Class  (weight = 1 per method; count of declared methods)
  CBO  - Coupling Between Objects    (distinct non-builtin PascalCase types referenced in the class body)
  RFC  - Response For a Class        (WMC + distinct qualified call sites  obj.Method()  in the class body)
  LCOM - Lack of Cohesion in Methods (Henderson-Sellers: 1 - avg_methods_per_field / total_methods)
           0 = perfectly cohesive, 1 = completely uncohesive

Layer detection (used by the ck-metrics job to apply thresholds):
  path contains /Domain/        -> "domain"
  path contains /Application/   -> "application"
  path contains /Infrastructure/ -> "infrastructure"
  path contains /Presentation/, /UI/, /Views/, /Controllers/ -> "presentation"
  anything else                 -> "other"

Output: JSON array on stdout, one object per class:
  [{"class":"Foo","file":"...","layer":"domain","wmc":N,"cbo":N,"rfc":N,"lcom":0.00}, ...]

Usage:
  python tools/check_ck_metrics.py <source-root>
  python tools/check_ck_metrics.py refactoring-examples

Limitations (inherent in regex-based analysis):
  - Cannot resolve type aliases or fully-qualified names.
  - Generic type parameters (T, TKey) may inflate CBO slightly.
  - LCOM uses field-name heuristics; auto-properties count as fields.
  - Partial classes in separate files are reported independently.
"""

import re
import sys
import json
from pathlib import Path
from collections import defaultdict

# ── Pre-processing ─────────────────────────────────────────────────────────────

_RE_BLOCK_COMMENT  = re.compile(r'/\*.*?\*/', re.DOTALL)
_RE_LINE_COMMENT   = re.compile(r'//[^\n]*')
_RE_VERBATIM_STR   = re.compile(r'@"(?:[^"]|"")*"', re.DOTALL)
_RE_INTERP_STR     = re.compile(r'\$"(?:[^"\\]|\\.)*"')
_RE_STRING         = re.compile(r'"(?:[^"\\]|\\.)*"')
_RE_CHAR_LIT       = re.compile(r"'(?:[^'\\]|\\.)'")


def _strip_noise(src: str) -> str:
    """Remove comments and string literals so patterns don't match inside them."""
    src = _RE_BLOCK_COMMENT.sub(' ', src)
    src = _RE_LINE_COMMENT.sub('', src)
    src = _RE_VERBATIM_STR.sub('""', src)
    src = _RE_INTERP_STR.sub('""', src)
    src = _RE_STRING.sub('""', src)
    src = _RE_CHAR_LIT.sub("' '", src)
    return src


# ── Structural extraction ──────────────────────────────────────────────────────

_RE_NAMESPACE = re.compile(r'\bnamespace\s+([\w.]+)')

_RE_CLASS = re.compile(
    r'\b(?:(?:public|internal|protected|private|abstract|sealed|static|partial)\s+)*'
    r'class\s+(\w+)\b'
)

_RE_INTERFACE_ENUM = re.compile(r'\b(?:interface|enum|struct|delegate|record)\s+\w+')


def _brace_body(text: str, start: int) -> str | None:
    """
    Return the text from the first '{' at or after `start` to its matching '}'.
    Returns None if no opening brace is found.
    """
    brace = text.find('{', start)
    if brace == -1:
        return None
    depth = 1
    pos   = brace + 1
    while pos < len(text) and depth:
        ch = text[pos]
        if   ch == '{': depth += 1
        elif ch == '}': depth -= 1
        pos += 1
    return text[brace:pos]


def _extract_classes(clean: str) -> list[tuple[str, str]]:
    """
    Return [(class_name, class_body), ...] for every class declaration found.
    Skips any match that is preceded by 'interface', 'enum', 'struct', etc.
    on the same logical line (avoids misidentifying nested types inside those).
    """
    results = []
    for m in _RE_CLASS.finditer(clean):
        # Ignore matches that are part of a base-list constraint (: SomeClass)
        preceding = clean[max(0, m.start() - 30):m.start()]
        if re.search(r'[:<,]\s*$', preceding.strip()):
            continue
        name = m.group(1)
        body = _brace_body(clean, m.end())
        if body is not None:
            results.append((name, body))
    return results


# ── WMC ───────────────────────────────────────────────────────────────────────
#
# A method is any member that has:
#   - one or more C# access / behaviour modifiers
#   - a return-type token (including void, generic, array, nullable)
#   - an identifier for its name
#   - an opening parenthesis '('
#
# We require at least one of the "primary" modifiers (public/private/protected/
# internal/static/abstract/virtual/override) to reduce false positives from
# local functions and lambdas.

_PRIMARY_MOD = (
    r'(?:public|private|protected|internal|static|abstract|virtual|override|sealed)'
)
_ANY_MOD = (
    r'(?:public|private|protected|internal|static|abstract|virtual|override|'
    r'sealed|async|new|extern|unsafe|partial|readonly)'
)
_RETURN_TYPE = r'(?:[\w<>\[\]?,\s]+?)'

_RE_METHOD = re.compile(
    rf'(?=.*{_PRIMARY_MOD})'                     # lookahead: must contain a primary modifier
    rf'(?:{_ANY_MOD}\s+)+'                        # one or more modifiers
    rf'{_RETURN_TYPE}\s'                          # return type
    rf'(\w+)\s*(?:<[^>]*?>)?\s*\(',              # MethodName( or MethodName<T>(
)

# Names that look like methods but are language keywords / property accessors
_METHOD_KEYWORDS = frozenset({
    'if', 'for', 'foreach', 'while', 'do', 'switch', 'catch', 'lock',
    'using', 'fixed', 'checked', 'unchecked', 'stackalloc',
    'return', 'throw', 'new', 'typeof', 'nameof', 'sizeof', 'default',
    'await', 'yield', 'get', 'set', 'init', 'add', 'remove', 'value',
    'when', 'where', 'select', 'from', 'join', 'group', 'into', 'orderby',
})


def _compute_wmc(body: str) -> int:
    count = 0
    for m in _RE_METHOD.finditer(body):
        name = m.group(1)
        if name and name.lower() not in _METHOD_KEYWORDS:
            count += 1
    return count


# ── CBO ───────────────────────────────────────────────────────────────────────
#
# Approximation: the set of distinct non-builtin PascalCase identifiers
# referenced inside the class body.  We also count namespaces brought in via
# 'using' directives at file level whose root is not a system namespace.

_RE_PASCAL = re.compile(r'\b([A-Z][A-Za-z0-9_]+)\b')

# Types that are part of the language / BCL and should not inflate CBO
_BUILTIN_TYPES = frozenset({
    # Primitives / BCL value types
    'Boolean', 'Byte', 'SByte', 'Char', 'Decimal', 'Double', 'Single',
    'Int16', 'Int32', 'Int64', 'UInt16', 'UInt32', 'UInt64', 'IntPtr',
    'UIntPtr', 'Object', 'String', 'Void',
    # Common generic collections
    'List', 'Dictionary', 'HashSet', 'Queue', 'Stack', 'LinkedList',
    'SortedDictionary', 'SortedSet', 'SortedList', 'ConcurrentDictionary',
    'ConcurrentQueue', 'ConcurrentStack', 'ConcurrentBag', 'ObservableCollection',
    # Common interfaces
    'IEnumerable', 'IEnumerator', 'IList', 'IDictionary', 'ICollection',
    'ISet', 'IReadOnlyList', 'IReadOnlyDictionary', 'IReadOnlyCollection',
    'IComparable', 'IEquatable', 'IDisposable', 'IAsyncDisposable',
    'IQueryable', 'ILookup', 'IGrouping',
    # Tasks / async
    'Task', 'ValueTask', 'CancellationToken', 'CancellationTokenSource',
    'Progress', 'IProgress',
    # Delegates / events
    'Action', 'Func', 'Predicate', 'EventHandler', 'EventArgs', 'Delegate',
    'MulticastDelegate',
    # Common BCL types
    'TimeSpan', 'DateTime', 'DateTimeOffset', 'DateOnly', 'TimeOnly',
    'Guid', 'Uri', 'Version', 'Tuple', 'ValueTuple', 'Nullable',
    'KeyValuePair', 'Lazy', 'WeakReference', 'Memory', 'Span',
    'ReadOnlyMemory', 'ReadOnlySpan', 'Array', 'Type', 'Attribute',
    'Enum', 'Flags', 'StringBuilder', 'Regex', 'Match', 'MatchCollection',
    'Path', 'File', 'Directory', 'FileInfo', 'DirectoryInfo', 'Stream',
    'StreamReader', 'StreamWriter', 'TextReader', 'TextWriter',
    'Exception', 'SystemException', 'ApplicationException',
    'ArgumentException', 'ArgumentNullException', 'ArgumentOutOfRangeException',
    'InvalidOperationException', 'NotImplementedException',
    'NullReferenceException', 'IndexOutOfRangeException',
    'NotSupportedException', 'OperationCanceledException',
    'FormatException', 'OverflowException', 'DivideByZeroException',
    'OutOfMemoryException', 'StackOverflowException', 'TimeoutException',
    'UnauthorizedAccessException', 'IOException', 'FileNotFoundException',
    # Attribute names that appear as [Something]
    'Serializable', 'Obsolete', 'Flags', 'NonSerialized',
    'DataMember', 'DataContract', 'JsonProperty', 'JsonIgnore',
    'Required', 'Range', 'StringLength', 'MaxLength', 'MinLength',
    # Common keywords that appear in PascalCase positions
    'True', 'False', 'Null', 'This', 'Base',
})

_SYSTEM_NS_ROOTS = frozenset({
    'System', 'Microsoft', 'Newtonsoft', 'NUnit', 'Xunit', 'Moq',
})

_RE_USING = re.compile(r'^\s*using\s+([\w.]+)\s*;', re.MULTILINE)


def _compute_cbo(file_src: str, class_name: str, class_body: str) -> int:
    """Count distinct non-builtin external types coupled to by the class."""
    types: set[str] = set()

    # 1. Non-system using directives at file scope indicate external coupling.
    for ns in _RE_USING.findall(file_src):
        root = ns.split('.')[0]
        if root not in _SYSTEM_NS_ROOTS:
            types.add(ns)

    # 2. PascalCase identifiers in the class body (field/param/return types, etc.)
    for m in _RE_PASCAL.finditer(class_body):
        name = m.group(1)
        if name == class_name:
            continue
        if name in _BUILTIN_TYPES:
            continue
        if len(name) < 2:
            continue
        types.add(name)

    return len(types)


# ── RFC ───────────────────────────────────────────────────────────────────────
#
# RFC = WMC + distinct external call sites.
# An external call site is any  identifier.MethodName(  pattern (at least one dot).
# We count unique  "receiver.method"  pairs to avoid counting the same call twice.

_RE_EXT_CALL = re.compile(r'\b(\w+(?:\.\w+)+)\s*\(')


def _compute_rfc(wmc: int, body: str) -> int:
    calls = {m.group(1) for m in _RE_EXT_CALL.finditer(body)}
    return wmc + len(calls)


# ── LCOM ──────────────────────────────────────────────────────────────────────
#
# Henderson-Sellers LCOM:  1 - ( sum(mf) / (M * F) )
#   F  = number of instance fields
#   M  = number of methods
#   mf = number of methods that access field f
#
# Field detection heuristic:
#   Line-level scan for patterns like:
#     private [readonly] [static] <type> _name;
#     private [readonly] [static] <type> name = ...;
#     protected ...
#   We capture the final identifier before ';' or '='.
#
# Method body extraction: brace-balanced from each method signature found by
# _RE_METHOD so that field references are attributed correctly per method.

_RE_FIELD_DECL = re.compile(
    r'(?:private|protected)\s+'           # must be non-public (instance state)
    r'(?:(?:static|readonly|volatile|new)\s+)*'
    r'(?!(?:class|interface|struct|enum|delegate|event|void|abstract|virtual|override)\b)'
    r'[\w<>\[\]?,.\s]+?'                  # type (non-greedy)
    r'\b(\w+)\s*'                         # field name
    r'(?:=[^;{{]*)?\s*;',                  # optional initialiser then ;
    re.MULTILINE,
)

# Accessor keywords that look like field names but are not
_FIELD_KEYWORDS = frozenset({
    'get', 'set', 'init', 'add', 'remove', 'value',
    'if', 'else', 'for', 'while', 'do', 'return', 'new',
})


def _extract_method_bodies_with_names(body: str) -> list[tuple[str, str]]:
    """Return [(method_name, method_body_text), ...] from a class body."""
    results = []
    for m in _RE_METHOD.finditer(body):
        name = m.group(1)
        if not name or name.lower() in _METHOD_KEYWORDS:
            continue
        # Skip past parameter list  (  ...  )
        depth = 0
        pos   = m.end() - 1   # points at '('
        while pos < len(body):
            if   body[pos] == '(': depth += 1
            elif body[pos] == ')':
                depth -= 1
                if depth == 0:
                    pos += 1
                    break
            pos += 1
        # Look ahead up to 200 chars for an opening brace
        # (skip whitespace, where-clauses, constraints, =>)
        lookahead = body[pos:pos + 300]
        brace_m   = re.search(r'\{', lookahead)
        if not brace_m:
            continue            # abstract / extern method — no body
        brace_abs = pos + brace_m.start()
        mb = _brace_body(body, brace_abs)
        if mb:
            results.append((name, mb))
    return results


def _compute_lcom(body: str) -> float:
    """
    Returns Henderson-Sellers LCOM in [0.0, 1.0].
    0.0 = perfectly cohesive, 1.0 = no cohesion.
    Returns 0.0 when the class has no fields or no methods (degenerate cases).
    """
    # Collect candidate field names
    raw_fields = [
        m.group(1)
        for m in _RE_FIELD_DECL.finditer(body)
        if m.group(1).lower() not in _FIELD_KEYWORDS
    ]
    # Deduplicate while preserving order
    seen: set[str] = set()
    fields = [f for f in raw_fields if not (f in seen or seen.add(f))]  # type: ignore[func-returns-value]

    method_bodies = _extract_method_bodies_with_names(body)

    F = len(fields)
    M = len(method_bodies)

    if F == 0 or M == 0:
        return 0.0

    total_mf = 0
    for field in fields:
        pat = re.compile(r'\b' + re.escape(field) + r'\b')
        mf  = sum(1 for _, mb in method_bodies if pat.search(mb))
        total_mf += mf

    avg_mf = total_mf / F
    lcom   = 1.0 - (avg_mf / M)
    return round(max(0.0, min(1.0, lcom)), 4)


# ── Layer detection ────────────────────────────────────────────────────────────

_LAYER_RULES: list[tuple[re.Pattern, str]] = [
    (re.compile(r'[/\\]Domain[/\\]',         re.IGNORECASE), 'domain'),
    (re.compile(r'[/\\]Application[/\\]',    re.IGNORECASE), 'application'),
    (re.compile(r'[/\\]Infrastructure[/\\]', re.IGNORECASE), 'infrastructure'),
    (re.compile(
        r'[/\\](?:Presentation|UI|Views?|Controllers?|ViewModels?)[/\\]',
        re.IGNORECASE
    ), 'presentation'),
]

_SKIP_DIRS = frozenset({
    'obj', 'bin', '.vs', 'Generated', 'Migrations',
    'node_modules', 'packages', '.git',
})


def _detect_layer(path: Path) -> str:
    s = str(path)
    for pattern, layer in _LAYER_RULES:
        if pattern.search(s):
            return layer
    return 'other'


# ── Per-file analysis ─────────────────────────────────────────────────────────

def analyse_file(path: Path) -> list[dict]:
    src   = path.read_text(encoding='utf-8', errors='ignore')
    clean = _strip_noise(src)
    layer = _detect_layer(path)

    classes = _extract_classes(clean)
    results = []
    for class_name, body in classes:
        wmc  = _compute_wmc(body)
        cbo  = _compute_cbo(src, class_name, body)
        rfc  = _compute_rfc(wmc, body)
        lcom = _compute_lcom(body)

        results.append({
            'class': class_name,
            'file':  str(path),
            'layer': layer,
            'wmc':   wmc,
            'cbo':   cbo,
            'rfc':   rfc,
            'lcom':  lcom,
        })
    return results


# ── Entry point ───────────────────────────────────────────────────────────────

def main() -> None:
    if len(sys.argv) < 2:
        print('Usage: check_ck_metrics.py <source-root>', file=sys.stderr)
        sys.exit(1)

    root = Path(sys.argv[1])
    if not root.exists():
        print(f'Source root not found: {root}', file=sys.stderr)
        sys.exit(1)

    all_results: list[dict] = []

    for cs_file in sorted(root.rglob('*.cs')):
        # Skip generated/build output directories
        if any(part in _SKIP_DIRS for part in cs_file.parts):
            continue
        # Skip generated files by naming convention
        if cs_file.name.endswith(('.Designer.cs', '.g.cs', '.generated.cs')):
            continue
        try:
            all_results.extend(analyse_file(cs_file))
        except Exception as exc:
            print(f'Warning: skipping {cs_file}: {exc}', file=sys.stderr)

    print(json.dumps(all_results, indent=2))


if __name__ == '__main__':
    main()
