namespace TenKingdoms;

public enum DisplayableObjectType { None, Town, Firm, FirmDie, Site, Unit, Bullet, Effect, Plant, Hill, Fire, Tornado }

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
            case DisplayableObjectType.FirmDie:
                renderer.DrawFirmDie(Sys.Instance.FirmDieArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Site:
                renderer.DrawSite(Sys.Instance.SiteArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Unit:
                renderer.DrawUnit(Sys.Instance.UnitArray[ObjectId], layer);
                break;
            case DisplayableObjectType.Bullet:
                renderer.DrawBullet(Sys.Instance.BulletArray[ObjectId]);
                break;
            case DisplayableObjectType.Effect:
                renderer.DrawEffect(Sys.Instance.EffectArray[ObjectId]);
                break;
            case DisplayableObjectType.Plant:
                renderer.DrawPlant(Sys.Instance.PlantRes.GetBitmap(ObjectId), DrawLocX, DrawLocY);
                break;
            case DisplayableObjectType.Hill:
                renderer.DrawHill(Sys.Instance.HillRes[ObjectId], layer, DrawLocX, DrawLocY);
                break;
            case DisplayableObjectType.Fire:
                renderer.DrawFlame();
                break;
            case DisplayableObjectType.Tornado:
                renderer.DrawTornado(Sys.Instance.TornadoArray[ObjectId]);
                break;
        }
    }
}
