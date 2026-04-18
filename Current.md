My Colors.axaml looks like this, please use this color file through the entire app 

<ResourceDictionary xmlns="https://github.com/avaloniaui"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
<!-- Primary Accent: The "Phrazie Yellow" -->
<Color x:Key="AccentYellow">#FFB800</Color>
<Color x:Key="AccentYellowDim">#4DFFB800</Color> <!-- 30% Opacity -->
<!-- Neutral Palette -->
<Color x:Key="SurfaceBlack">#0A0A0B</Color>
<Color x:Key="SurfaceDark">#141416</Color>
<Color x:Key="SurfaceLight">#1C1C1E</Color>
<Color x:Key="BorderMuted">#2C2C2E</Color>
<!-- Semantic Text Colors -->
<Color x:Key="TextEnabled">#FFFFFF</Color>
<Color x:Key="TextDisabled">#48484A</Color>
<Color x:Key="TextMuted">#8E8E93</Color>
<!-- Brushes for UI Elements -->
<!-- Backgrounds -->
<SolidColorBrush x:Key="MainBackgroundBrush" Color="{StaticResource
SurfaceBlack}"/>
<SolidColorBrush x:Key="PanelBackgroundBrush" Color="{StaticResource
SurfaceDark}"/>
<SolidColorBrush x:Key="ControlBackgroundBrush" Color="{StaticResource
SurfaceLight}"/>
<!-- Box/Control States -->
<!-- Default -->
<SolidColorBrush x:Key="BoxBackgroundDefault" Color="{StaticResource
SurfaceLight}"/>
<SolidColorBrush x:Key="BoxBorderDefault" Color="{StaticResource BorderMuted}"/>
<!-- Focused/Active -->
<SolidColorBrush x:Key="BoxBorderFocused" Color="{StaticResource AccentYellow}"/
>
<SolidColorBrush x:Key="BoxBackgroundFocused" Color="{StaticResource
SurfaceLight}"/>
<!-- Disabled -->
<SolidColorBrush x:Key="BoxBackgroundDisabled" Color="#121214"/>
<SolidColorBrush x:Key="BoxBorderDisabled" Color="#1F1F21"/>
<!-- Text Brushes -->
<SolidColorBrush x:Key="TextBrushPrimary" Color="{StaticResource TextEnabled}"/>
<SolidColorBrush x:Key="TextBrushSecondary" Color="{StaticResource TextMuted}"/>
<SolidColorBrush x:Key="TextBrushDisabled" Color="{StaticResource
TextDisabled}"/>
<SolidColorBrush x:Key="TextBrushAccent" Color="{StaticResource AccentYellow}"/>
</ResourceDictionary>
