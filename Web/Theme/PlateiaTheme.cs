using MudBlazor;

namespace Web.Theme;

/// <summary>
/// Design system Plateia — roxo teatral, ciano SaaS e dourado spotlight (identidade da logo).
/// </summary>
public static class PlateiaTheme
{
    public const string Sand50 = "#FAF8F5";
    public const string White = "#FFFFFF";
    public const string Violet = "#6B21A8";
    public const string VioletLight = "#7C3AED";
    public const string Teal = "#0D9488";
    public const string TealLight = "#14B8A6";
    public const string Gold = "#C9A962";
    public const string GoldDark = "#A8863F";
    public const string Slate900 = "#0F172A";
    public const string Slate400 = "#94A3B8";
    public const string Slate200 = "#E2E8F0";
    public const string SeatVip = VioletLight;
    public const string SeatGeral = TealLight;
    public const string SeatOccupied = "#CBD5E1";
    public const string SeatSelected = Gold;

    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = Slate900,
            White = White,
            Background = Sand50,
            BackgroundGray = "#F3F0EB",
            Surface = White,
            DrawerBackground = White,
            DrawerText = Slate900,
            AppbarBackground = White,
            AppbarText = Slate900,
            Primary = Violet,
            PrimaryContrastText = White,
            PrimaryDarken = "#581C87",
            PrimaryLighten = VioletLight,
            Secondary = Teal,
            SecondaryContrastText = White,
            SecondaryDarken = "#0F766E",
            SecondaryLighten = TealLight,
            Tertiary = Gold,
            TertiaryContrastText = "#422006",
            TextPrimary = Slate900,
            TextSecondary = "#475569",
            TextDisabled = Slate400,
            ActionDefault = "#64748B",
            ActionDisabled = Slate400,
            ActionDisabledBackground = "#F3F0EB",
            Divider = Slate200,
            DividerLight = "#F3F0EB",
            TableLines = Slate200,
            LinesDefault = Slate200,
            LinesInputs = "#CBD5E1",
            Dark = Slate400,
            DarkContrastText = White,
            Success = "#059669",
            Info = TealLight,
            Warning = Gold,
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
