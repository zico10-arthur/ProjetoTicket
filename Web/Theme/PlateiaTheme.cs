using MudBlazor;

namespace Web.Theme;

/// <summary>
/// Design system Plateia — indigo + coral, modo claro híbrido.
/// </summary>
public static class PlateiaTheme
{
    public const string Slate50 = "#F8FAFC";
    public const string White = "#FFFFFF";
    public const string Indigo = "#4F46E5";
    public const string Coral = "#F97316";
    public const string Slate900 = "#0F172A";
    public const string Slate400 = "#94A3B8";
    public const string Slate200 = "#E2E8F0";
    public const string SeatVip = "#7C3AED";
    public const string SeatGeral = "#6366F1";
    public const string SeatOccupied = "#CBD5E1";
    public const string SeatSelected = "#F97316";

    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = Slate900,
            White = White,
            Background = Slate50,
            BackgroundGray = "#F1F5F9",
            Surface = White,
            DrawerBackground = White,
            DrawerText = Slate900,
            AppbarBackground = White,
            AppbarText = Slate900,
            Primary = Indigo,
            PrimaryContrastText = White,
            PrimaryDarken = "#4338CA",
            PrimaryLighten = "#6366F1",
            Secondary = Coral,
            SecondaryContrastText = White,
            TextPrimary = Slate900,
            TextSecondary = "#475569",
            TextDisabled = Slate400,
            ActionDefault = "#64748B",
            ActionDisabled = Slate400,
            ActionDisabledBackground = "#F1F5F9",
            Divider = Slate200,
            DividerLight = "#F1F5F9",
            TableLines = Slate200,
            LinesDefault = Slate200,
            LinesInputs = "#CBD5E1",
            Dark = Slate400,
            DarkContrastText = White,
            Success = "#059669",
            Info = "#0284C7",
            Warning = "#D97706",
            Error = "#DC2626",
            HoverOpacity = 0.04,
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Plus Jakarta Sans", "system-ui", "Segoe UI", "sans-serif" },
            },
            H1 = new H1Typography { FontWeight = "700" },
            H2 = new H2Typography { FontWeight = "700" },
            H3 = new H3Typography { FontWeight = "700" },
            H4 = new H4Typography { FontWeight = "700" },
            H5 = new H5Typography { FontWeight = "600" },
            H6 = new H6Typography { FontWeight = "600" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "600" },
        },
    };
}
