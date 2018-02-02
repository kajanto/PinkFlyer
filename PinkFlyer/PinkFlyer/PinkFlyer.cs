using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;
using Jypeli.Widgets;

/// @author Tomi Nyyssönen, Juho Kajanto
/// @version 13.11.2015 
/// @version 29.11.2015, lisättiin alkuvalikko
/// @version 9.12.2015, tehtiin korjaukset
/// <summary>
/// PinkFlyer
/// </summary>
// TODO: Lisää kenttiä
// TODO: Loppuvastus
// TODO: Parempi vihollisten käyttäytyminen
public class PinkFlyer : PhysicsGame
{
    private Vector nopeusYlos = new Vector(0, 800);
    private Vector nopeusAlas = new Vector(0, -800);
    private Vector nopeusOikealle = new Vector(800, 0);
    private Vector nopeusVasemmalle = new Vector(-800, 0);

    private Image taustakuva = LoadImage("background");
    private Image pelaajahahmo = LoadImage("pink_guy_vaaka");
    private Image ammus = LoadImage("cromosomi");

    private static Image[] vihut = Game.LoadImages("almond1", "almond2", "almond3");

    private PhysicsObject oikeaReuna;
    private PhysicsObject pelaaja;

    private IntMeter pisteLaskuri;

    private static string[] taso0 = {
        "                         ",
        "                    *   *",
        "                         ",
        "        ~             *  ",
        "                         ",
        "                    *   *",
        "                         " };

    private static string[] taso1 = {
        "                         ",
        "                      *  ",
        "                         ",
        "        ~           *   *",
        "                         ",
        "                      *  ",
        "                         " };

    private static string[][] tasolista = {taso0, taso1};

    private double tileWidth;
    private double tileHeight;

    private const double AMPUMATAAJUUS = 0.2;
    private const int AMMUKSEN_NOPEUS = 800;
    private const int VIHU_TUHOTTU_PISTEET = 50;
    private const int VIHU_SELVISI_PISTEET = 20;
    private const int PELAAJA_OSUU_VIHUUN = 75;

    private IntMeter vihujenLkm;

    /// <summary>
    /// Paras peli ikinä!
    /// </summary>
    public override void Begin()
    {
        Valikko();
    }


    /// <summary>
    /// Luo alkuvalikon peliin
    /// </summary>
    void Valikko()
    {
        Mouse.IsCursorVisible = true;
        ClearAll();

        List<Label> valikonKohdat = new List<Label>();

        Label kohta1 = new Label("Begin adventure!");
        kohta1.Position = new Vector(0, 20);
        valikonKohdat.Add(kohta1);
        
        Label kohta2 = new Label("Go do something more useful");
        kohta2.Position = new Vector(0, -20);
        valikonKohdat.Add(kohta2);

        foreach (Label valikonKohta in valikonKohdat)
        {
            Add(valikonKohta);
        }

        Mouse.ListenOn(kohta1, MouseButton.Left, ButtonState.Pressed, AloitaPeli, null);
        Mouse.ListenOn(kohta2, MouseButton.Left, ButtonState.Pressed, Exit, null);
    }


    /// <summary>
    /// Aloittaa pelin.
    /// </summary>
    void AloitaPeli()
    {
        Mouse.IsCursorVisible = false;
        ClearAll();
        LuoKentta();
        SetKeyConfig();
        LuoPistelaskuri();
    }


    /// <summary>
    /// Luodaan pelikenttä.
    /// </summary>
    void LuoKentta()
    {
        Level.Background.Image = taustakuva;
        IsFullScreen = true;
        Level.Width = 2*Screen.Width;
        Level.Height = 1.07*Screen.Height;
        Level.CreateBorders(false);

        Camera.X = Level.Left + Screen.Width;
        Camera.Zoom(1);

        oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.Restitution = 0.0;
        oikeaReuna.IsVisible = false;

        PhysicsObject vasenReuna = Level.CreateLeftBorder();
        vasenReuna.Restitution = 0.0;
        vasenReuna.IsVisible = false;
        AddCollisionHandler(vasenReuna, TormaysReunaan);
        
        tileWidth = Level.Width / taso0[0].Length;
        tileHeight = Level.Height / taso0.Length;
        TileMap tiles = TileMap.FromStringArray(tasolista[0]);

        vihujenLkm = new IntMeter(0);

        tiles['*'] = LisaaManteli;
        tiles['~'] = LisaaPelaaja; 
        tiles.Insert(tileWidth, tileHeight);
        
        Level.Background.ScaleToLevelFull();

        //MediaPlayer.Play("33 Gibe de pusi b0ss");
        //MediaPlayer.IsRepeating = true;
    }


    /// <summary>
    /// Asetetaan näppäimet.
    /// </summary>
    void SetKeyConfig()
    {
        Keyboard.Listen(Key.Up, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusYlos);
        Keyboard.Listen(Key.Up, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);
        
        Keyboard.Listen(Key.Down, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa alas", pelaaja, nopeusAlas);
        Keyboard.Listen(Key.Down, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);
        
        Keyboard.Listen(Key.Left, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusVasemmalle);
        Keyboard.Listen(Key.Left, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);
        
        Keyboard.Listen(Key.Right, ButtonState.Down, AsetaNopeus, "Liikuta pelaajaa ylös", pelaaja, nopeusOikealle);
        Keyboard.Listen(Key.Right, ButtonState.Released, AsetaNopeus, null, pelaaja, Vector.Zero);

        Timer fireRate = new Timer();
        fireRate.Interval = AMPUMATAAJUUS;
        fireRate.Timeout += Ammu;

        Keyboard.Listen(Key.Space, ButtonState.Pressed, fireRate.Start, "Ampuu");
        Keyboard.Listen(Key.Space, ButtonState.Pressed, Ammu, "Ampuu");
        Keyboard.Listen(Key.Space, ButtonState.Released, fireRate.Stop, "");
        
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä näppäinohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Luodaan pistelaskuri
    /// </summary>
    void LuoPistelaskuri()
    {
        pisteLaskuri = new IntMeter(0);
        Label pisteNaytto = new Label();
        pisteNaytto.X = Screen.Left + Screen.Width/2;
        pisteNaytto.Y = Screen.Top - 30;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.Title = "Points";
        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
    }


    /// <summary>
    /// Luodaan pelaajan ampumat ammukset.
    /// </summary>
    void Ammu()
    {
        PhysicsObject panos = new PhysicsObject(0.2 * tileWidth, 0.15 * tileHeight, Shape.Rectangle);
        panos.Position = pelaaja.Position + new Vector(1.25 * tileWidth, 0);
        panos.Hit(new Vector(AMMUKSEN_NOPEUS, 0));
        AddCollisionHandler(panos, Tormays);
        panos.Image = ammus;
        Add(panos);
    }


    /// <summary>
    /// Käsittelee panosten törmäyksiä.
    /// </summary>
    /// <param name="panos">Pelaajan ampuma panos.</param>
    /// <param name="kohde">Asia mihin panos osuu.</param>
    public void Tormays(PhysicsObject panos, PhysicsObject kohde)
    {
        if (kohde == oikeaReuna)
        {
            panos.Destroy();
        }

        if (kohde.Tag.Equals("vihu"))
        {
            kohde.Destroy();
            panos.Destroy();
            pisteLaskuri.Value += VIHU_TUHOTTU_PISTEET;
            vihujenLkm.Value--;
            if (vihujenLkm.Value == 0)
            {
                LopetaPeli();
            }
        }   
    }


    /// <summary>
    /// Käsittelee törmäykset reunoihin
    /// </summary>
    /// <param name="reuna">mikä reuna</param>
    /// <param name="kohde">mikä osuu</param>
    public void TormaysReunaan(PhysicsObject reuna, PhysicsObject kohde)
    {
        if (kohde.Tag.Equals("vihu"))
        {
            kohde.Destroy();
            pisteLaskuri.Value -= VIHU_SELVISI_PISTEET;
            vihujenLkm.Value--;
            if (vihujenLkm.Value == 0)
            {
                LopetaPeli();
            }
        } 
    }


    /// <summary>
    /// Luodaan pelaajahahmo.
    /// </summary>
    /// <returns>Pelaajahahmo</returns>
    PhysicsObject LisaaPelaaja()
    {
        pelaaja = new PhysicsObject(2.5*tileWidth, 0.7*tileHeight);
        pelaaja.Shape = Shape.Rectangle;
        pelaaja.Restitution = 0.0;
        pelaaja.Image = pelaajahahmo;
        pelaaja.CanRotate = false;
        pelaaja.Tag = "pelaaja";
        AddCollisionHandler(pelaaja, TormasiPelaajaan);
        Add(pelaaja);
        return pelaaja;
    }


    /// <summary>
    /// Käsittelee pelaajan törmäyksiä
    /// </summary>
    /// <param name="pelaaja">Kuka törmäsi</param>
    /// <param name="kohde">Mihin törmäsi</param>
    public void TormasiPelaajaan(PhysicsObject pelaaja, PhysicsObject kohde)
    {
        if (kohde.Tag.Equals("vihu"))
        {
            pisteLaskuri.Value -= PELAAJA_OSUU_VIHUUN;
            pelaaja.Destroy();
            kohde.Destroy();
            LopetaPeli();
        }
    }


    /// <summary>
    /// Luodaan vihollishahmo
    /// </summary>
    /// <returns>Vihollishahmo</returns>
    public PhysicsObject LisaaManteli()
    {
        PhysicsObject vihu = new PhysicsObject(tileWidth, tileHeight);
        vihu.Shape = Shape.Circle;
        vihu.Restitution = 0.0;
        vihu.Image = RandomGen.SelectOne<Image>(vihut);
        vihu.CanRotate = false;
        vihu.Tag = "vihu";
        vihu.Hit(new Vector(-250, 0));
        vihu.Oscillate(Vector.UnitY, 100, 0.8);
        vihujenLkm.Value += 1;
        Add(vihu);
        return vihu;
    }


    /// <summary>
    /// Asettaa pelaajahahmolle nopeuden.
    /// </summary>
    /// <param name="pelaaja">Pelaajahahmo</param>
    /// <param name="nopeus">Liikkeen nopeus</param>
    void AsetaNopeus(PhysicsObject pelaaja, Vector nopeus)
    {
        if ((nopeus.Y > 0) && (pelaaja.Top > Screen.Top))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        if ((nopeus.Y < 0) && (pelaaja.Bottom < Screen.Bottom))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        if ((nopeus.X > 0) && (pelaaja.Right > Screen.Right))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        if ((nopeus.X < 0) && (pelaaja.Left < Screen.Left))
        {
            pelaaja.Velocity = Vector.Zero;
            return;
        }

        pelaaja.Velocity = nopeus;
    }


    /// <summary>
    /// Pelin lopetus
    /// </summary>
    public void LopetaPeli()
    {
        MultiSelectWindow lopetus = new MultiSelectWindow("game over b0ss", "Begin adventure!", "Go do something more useful");
        lopetus.ItemSelected += ValikonNappiaPainettiin;
        lopetus.SetButtonTextColor(Color.Black);
        lopetus.Color = Color.HotPink;
        lopetus.SelectionColor = Color.Pink;
        Add(lopetus);
    }


    /// <summary>
    /// Käsittelee loppuvalikon nappien painalluksia.
    /// </summary>
    /// <param name="nappi">monesko nappi ylhäältä, jota painettiin</param>
    void ValikonNappiaPainettiin(int nappi)
    {
        switch (nappi)
        {
            case 0:
                AloitaPeli();
                break;
            case 1:
                Exit();
                break;
        }
    }
}
