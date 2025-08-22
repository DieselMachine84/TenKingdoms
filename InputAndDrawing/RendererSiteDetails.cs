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
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 48);
        DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 53);
        switch (site.SiteType)
        {
            case Site.SITE_RAW:
                DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 82);
                PutText(FontSan, "Resource", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, RawRes[site.ObjectId].name, DetailsX1 + 108, DetailsY1 + 56, -1, true);
                PutText(FontSan, "Reserve", DetailsX1 + 13, DetailsY1 + 85, -1, true);
                PutText(FontSan, site.ReserveQty.ToString(), DetailsX1 + 108, DetailsY1 + 86, -1, true);
                break;

            case Site.SITE_SCROLL:
                DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 82);
                GodInfo godInfo = GodRes[site.ObjectId];
                PutText(FontSan, "Nationality", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, RaceRes[godInfo.race_id].name, DetailsX1 + 108, DetailsY1 + 56, -1, true);
                PutText(FontSan, "Invoke", DetailsX1 + 13, DetailsY1 + 85, -1, true);
                PutText(FontSan, UnitRes[godInfo.unit_id].name, DetailsX1 + 108, DetailsY1 + 85, -1, true);
                break;

            case Site.SITE_GOLD_COIN:
                PutText(FontSan, "Treasure", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, site.ObjectId.ToString(), DetailsX1 + 108, DetailsY1 + 57, -1, true);
                break;
        }
    }
}