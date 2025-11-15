namespace TenKingdoms;

public enum DisplayableObjectType { None, Town, Firm, Site, Unit, Plant, Hill }

public class DisplayableObject
{
    public DisplayableObjectType ObjectType { get; set; }
    public int ObjectId { get; set; }
    public int DrawLocX { get; set; }
    public int DrawLocY { get; set; }
    public int DrawY2 { get; set; }
    
    public void Draw(IRenderer renderer, int layer)
    {
        switch (ObjectType)
        {
            case DisplayableObjectType.Town:
                renderer.DrawTown(Sys.Instance.TownArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Firm:
                renderer.DrawFirm(Sys.Instance.FirmArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Site:
                renderer.DrawSite(Sys.Instance.SiteArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Unit:
                renderer.DrawUnit(Sys.Instance.UnitArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Plant:
                renderer.DrawPlant(Sys.Instance.PlantRes.GetBitmap(ObjectId), DrawLocX, DrawLocY);
                break;
            case DisplayableObjectType.Hill:
                renderer.DrawHill(Sys.Instance.HillRes[ObjectId], layer, DrawLocX, DrawLocY);
                break;
        }
    }
}
