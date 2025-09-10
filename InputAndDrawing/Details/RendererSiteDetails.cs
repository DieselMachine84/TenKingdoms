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
        
        switch (site.SiteType)
        {
            case Site.SITE_RAW:
                DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 48);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 53);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 82);
                PutText(FontSan, "Resource", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, RawRes[site.ObjectId].name, DetailsX1 + 113, DetailsY1 + 56, -1, true);
                PutText(FontSan, "Reserve", DetailsX1 + 13, DetailsY1 + 85, -1, true);
                PutText(FontSan, site.ReserveQty.ToString(), DetailsX1 + 113, DetailsY1 + 87, -1, true);
                break;

            case Site.SITE_SCROLL:
                DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 48);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 53);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 82);
                GodInfo godInfo = GodRes[site.ObjectId];
                PutText(FontSan, "Nationality", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, RaceRes[godInfo.race_id].name, DetailsX1 + 113, DetailsY1 + 56, -1, true);
                PutText(FontSan, "Invoke", DetailsX1 + 13, DetailsY1 + 85, -1, true);
                PutText(FontSan, UnitRes[godInfo.unit_id].name, DetailsX1 + 113, DetailsY1 + 85, -1, true);
                break;

            case Site.SITE_GOLD_COIN:
                DrawPanelWithOneField(DetailsX1 + 2, DetailsY1 + 48);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 53);
                PutText(FontSan, "Worth", DetailsX1 + 13, DetailsY1 + 56, -1, true);
                PutText(FontSan, "$" + site.ObjectId, DetailsX1 + 113, DetailsY1 + 58, -1, true);
                break;
        }
    }
}