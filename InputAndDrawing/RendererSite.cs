using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawSiteDetails(Site site)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);

        string siteText = site.SiteType switch
        {
            Site.SITE_RAW => "Natural Resource",
            Site.SITE_SCROLL => "Scroll of Power",
            Site.SITE_GOLD_COIN => "Treasure",
            _ => String.Empty
        };
        PutTextCenter(FontSan, siteText, DetailsX1 + 2, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);
    }
}