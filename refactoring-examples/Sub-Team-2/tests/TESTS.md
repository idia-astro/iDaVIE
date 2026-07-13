# Sub-Team 2 — Test Files

| File | Tests | Type |
|---|---|---|
| `FitsHeaderUnitTests.cs` | T-01 to T-03 | Unit · White-box |
| `NativePluginLoaderUnitTests.cs` | T-04 to T-05 | Unit · White-box |
| `FitsReaderIntegrationTests.cs` | T-06 to T-09 | Integration · Black-box |
| `StructuralTests.cs` | T-10 to T-12 | Structural · White-box |

Full descriptions in `docs/sub-team-2/test-strategy.md`.

```bash
dotnet test --filter "Category!=Integration"   # no DLL needed
dotnet test                                     # full suite — needs idavie_fits.dll
```
