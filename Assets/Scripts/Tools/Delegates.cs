using CatalogData;
using VolumeData;

public delegate void StringDelegate(string value);
public delegate void ColorMapDelegate(ColorMapEnum colorMap);

public delegate void VolumeDataSetRendererDelegate(VolumeDataSetRenderer dataSet);
public delegate void CatalogDataSetRendererDelegate(CatalogDataSetRenderer dataSet);