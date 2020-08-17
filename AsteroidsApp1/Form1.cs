using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AsteroidsApp1
{
    public partial class Form1 : Form
    {
        const int SIZE_X = 640;
        const int SIZE_Y = 480;
        const int SHIP_SIZE = 30;
        const int SHIP_TURN_SPD = 270;
        const int SHIP_THRUST = 5;
        const float FRICTION = 0.5f;
        const int ROID_SIZE = 100; // starting size of asteroids in pixels
        const int FPS = 30;
        const int ROID_SPD = 60; // max starting speed of asteroids in pixels per second
        const int ROID_VERT = 10; // average number of vertices on each asteroid
        const int ROID_NUM = 3; // starting number of asteroids
        const int GAME_LIVES = 3; // starting number of lives
        const int ROID_PTS_LGE = 20; // points scored for a large asteroid
        const int ROID_PTS_MED = 50; // points scored for a medium asteroid
        const int ROID_PTS_SML = 100; // points scored for a small asteroid
        const float SHIP_EXPLODE_DUR = 0.3f; // duration of the ship's explosion in seconds
        const float SHIP_BLINK_DUR = 0.1f; // duration in seconds of a single blink during ship's invisibility
        const float LASER_EXPLODE_DUR = 0.1f; // duration of the lasers' explosion in seconds
        const int LASER_MAX = 10; // maximum number of lasers on screen at once
        const int LASER_SPD = 500; // speed of lasers in pixels per second
        const float LASER_DIST = 0.6f; // max distance laser can travel as fraction of screen width
        const int SHIP_INV_DUR = 3; // duration of the ship's invisibility in seconds

        int roidsTotal = 0;
        int roidsLeft = 0;
        int level = 0;
        int lives = 0;
        int score = 0;
        int scoreForLife = 0;

        PointF[] ShipFigure = new PointF[4];
        PointF[] ThrustingFigure = new PointF[3];

        Bitmap finalImage = new Bitmap(SIZE_X, SIZE_Y);
        Graphics gfx;
        struct ShipStruct
        {
            public float x;
            public float y;
            public float a;
            public int r;
            public int blinkNum;
            public int blinkTime;
            public bool canShoot;
            public bool dead;
            public int explodeTime;
            public List<LaserStruct> lasers;
            public float rot;
            public bool thrusting;
            public float thrustX;
            public float thrustY;
        }
        ShipStruct ship;
        bool ThrustShowFlag = false;
        struct AsteroidStruct
        {
            public float x;
            public float y;
            public float xv;
            public float yv;
            public int a;
            public float r;
            public int rot;
        }
        List<AsteroidStruct> roids = new List<AsteroidStruct>();
        struct LaserStruct
        {
            public float x;
            public float y;
            public float xv;
            public float yv;
            public float dist;
            public int explodeTime;
        }
        Random rndNewAsteroid = new Random();
        Bitmap asteroid = res.Asteroid;
        Bitmap shipBMP = res.Ship_1;
        //#####################################################################################################################

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pic.SetBounds(0, 0, SIZE_X, SIZE_Y);
            pic.BackgroundImage = res.kosmos;
            this.ClientSize = new Size(SIZE_X, SIZE_Y);
            this.Text = "Астероїди від Yozhega";
            gfx = Graphics.FromImage(finalImage); // pictureBox1.CreateGraphics() - Галімий метод

            newGame(); 

            tmrMain.Interval = 1000 / FPS;
            tmrMain.Enabled = true;
        }

        private void newGame()
        {
            level = 0;
            lives = GAME_LIVES;
            score = 0;
            scoreForLife = 0;
            ship = NewShip();

            NewLevel();
        }

        private void NewLevel()
        {
            createAsteroidBelt();
        }

        private void PlaySoundFromRes()
        {
            System.Media.SoundPlayer snd = new System.Media.SoundPlayer(res.Drill);
            snd.Play();
        }

        private ShipStruct NewShip()
        {
            return new ShipStruct
            {
                x = 320,
                y = 240,
                a = 90f / 180f * (float)Math.PI,
                r = SHIP_SIZE / 2,
                blinkNum = (int)Math.Ceiling(SHIP_INV_DUR / SHIP_BLINK_DUR),
                blinkTime = (int)Math.Ceiling(SHIP_BLINK_DUR * FPS),
                canShoot = true,
                dead = false,
                explodeTime = 0,
                lasers = new List<LaserStruct>(),
                rot = 0,
                thrusting = false,
                thrustX = 0,
                thrustY = 0
            };
        }

        private Bitmap RotateImage(Image img, float rotationAngle)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            Graphics gfxx = Graphics.FromImage(bmp);
            gfxx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
            gfxx.RotateTransform(rotationAngle);
            gfxx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
            gfxx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfxx.DrawImage(img, 0,0, img.Width, img.Height);
            gfxx.Dispose();
            return bmp;
        }

        private void UpdateScene()
        {
            bool blinkOn = ship.blinkNum % 2 == 0;
            bool exploding = ship.explodeTime > 0;

            // gfx.DrawImage(Bitmap, new Rectangle(x, y, w, h));
            gfx.Clear(Color.Transparent);

            // Paint Asteroids
            DrawTheAsteroids();

            // thrust the ship
            if (ship.thrusting && !ship.dead)
            {
                ship.thrustX += SHIP_THRUST * (float)Math.Cos(ship.a) / FPS;
                ship.thrustY -= SHIP_THRUST * (float)Math.Sin(ship.a) / FPS;


            }
            else
            {
                // apply friction (slow the ship down when not thrusting)
                ship.thrustX -= FRICTION * ship.thrustX / FPS;
                if (Math.Abs(ship.thrustX) < 0.01f) ship.thrustX = 0;
                ship.thrustY -= FRICTION * ship.thrustY / FPS;
                if (Math.Abs(ship.thrustY) < 0.01f) ship.thrustY = 0;
            }



            // paint the ship
            if (!exploding)
            {
                if (blinkOn && !ship.dead)
                {
                    //drawShip(ship.x, ship.y, ship.a);
                    //PaintShip();

                    gfx.DrawImage(RotateImage(shipBMP, ship.a / (float)Math.PI * 180*-1+90), ship.x - ship.r, ship.y - ship.r, ship.r * 2, ship.r * 2);
                }

                // handle blinking
                if (ship.blinkNum > 0)
                {

                    // reduce the blink time
                    ship.blinkTime--;

                    // reduce the blink num
                    if (ship.blinkTime == 0)
                    {
                        ship.blinkTime = (int)Math.Ceiling(SHIP_BLINK_DUR * FPS);
                        ship.blinkNum--;
                    }
                }
            }
            else
            {
                // draw the explosion
                gfx.DrawImage(res.BOOM, ship.x - ship.r, ship.y - ship.r, ship.r * 2, ship.r * 2);
            }

            // draw the lasers
            for (var i = 0; i < ship.lasers.Count; i++)
            {
                if (ship.lasers[i].explodeTime == 0)
                    gfx.DrawArc(new Pen(Color.White, 1), ship.lasers[i].x, ship.lasers[i].y, SHIP_SIZE / 15f, SHIP_SIZE / 15f, 0, 360);
                else
                    gfx.DrawArc(new Pen(Color.OrangeRed, 7), ship.lasers[i].x, ship.lasers[i].y, ship.r * 0.5f, ship.r * 0.5f, 0, 360);
            }

            // detect laser hits on asteroids
            float ax, ay, ar, lx, ly;
            for (int i = roids.Count - 1; i >= 0; i--)
            {
                // loop over the lasers
                for (int j = ship.lasers.Count - 1; j >= 0; j--)
                {
                    // detect hits
                    if (ship.lasers[j].explodeTime == 0 && distBetweenPoints(roids[i].x, roids[i].y, ship.lasers[j].x, ship.lasers[j].y) < roids[i].r)
                    {
                        // destroy the asteroid and activate the laser explosion
                        destroyAsteroid(i);
                        LaserStruct tmp = ship.lasers[j];
                        tmp.explodeTime = (int)Math.Ceiling(LASER_EXPLODE_DUR * FPS);
                        ship.lasers[j] = tmp;
                        break;
                    }
                }
            }

            // handle edge of screen
            HandleEdgeOfScreen();

            // move the lasers
            for (var i = ship.lasers.Count - 1; i >= 0; i--)
            {
                LaserStruct tmp = ship.lasers[i];

                // check distance travelled
                if (tmp.dist > LASER_DIST * SIZE_X)
                {
                    //ship.lasers.splice(i, 1);
                    ship.lasers.RemoveAt(i);
                    continue;
                }

                // handle the explosion
                if (ship.lasers[i].explodeTime > 0)
                {
                    tmp.explodeTime--;

                    // destroy the laser after the duration is up
                    if (tmp.explodeTime == 0)
                    {
                        //ship.lasers.splice(i, 1);
                        ship.lasers.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // move the laser
                    tmp.x += tmp.xv;
                    tmp.y += tmp.yv;

                    // calculate the distance travelled
                    tmp.dist += (float)Math.Sqrt(Math.Pow(tmp.xv, 2) + Math.Pow(tmp.yv, 2));
                }

                // handle edge of screen
                if (tmp.x < 0)
                {
                    tmp.x = SIZE_X;
                }
                else if (tmp.x > SIZE_X)
                {
                    tmp.x = 0;
                }
                if (tmp.y < 0)
                {
                    tmp.y = SIZE_Y;
                }
                else if (tmp.y > SIZE_Y)
                {
                    tmp.y = 0;
                }

                ship.lasers[i] = tmp;
            }

            // Move asteroids 
            for (var i = 0; i < roids.Count; i++)
            {
                AsteroidStruct tmp = roids[i];
                tmp.x += roids[i].xv;
                tmp.y += roids[i].yv;
                
                // handle asteroid edge of screen
                if (roids[i].x < 0 - roids[i].r) tmp.x= SIZE_X + roids[i].r; else if (roids[i].x > SIZE_X + roids[i].r) tmp.x = 0 - roids[i].r;
                if (roids[i].y < 0 - roids[i].r) tmp.y = SIZE_Y + roids[i].r; else if (roids[i].y > SIZE_Y + roids[i].r) tmp.y = 0 - roids[i].r;

                roids[i] = tmp;
            }

            // show ship collision
            // gfx.DrawArc(new Pen(Color.Red, 1), ship.x - (SHIP_SIZE / 2), ship.y - (SHIP_SIZE / 2), SHIP_SIZE, SHIP_SIZE, 0, 360);

            PaintString("Кораблі: " + lives, 10, 10);
            PaintString("Рівень: " + (level + 1), 10, 50);

            //if (DateTime.Now.Second == 0 && SoundFlag) { PlaySoundFromRes(); SoundFlag = false; }
            //if (DateTime.Now.Second == 1) SoundFlag = true;
            if (!exploding)
            {
                // only check when not blinking
                if (ship.blinkNum == 0 && !ship.dead)
                {
                    for (var i = 0; i < roids.Count; i++)
                    {
                        if (distBetweenPoints(ship.x, ship.y, roids[i].x, roids[i].y) < ship.r + roids[i].r)
                        {
                            explodeShip();
                            destroyAsteroid(i);
                            break;
                        }
                    }
                }

                // paint thrusting
                if (ship.thrusting && !ship.dead) PaintShipThrust();

                // rotate the ship
                ship.a += ship.rot;

                // move the ship
                ship.x += ship.thrustX;
                ship.y += ship.thrustY;
            }
            else
            {
                ship.explodeTime--;

                // reset the ship after the explosion has finished
                if (ship.explodeTime == 0)
                {
                    lives--;
                    if (lives == 0)
                    {
                        gameOver();
                    }
                    else
                    {
                        ship = NewShip();
                    }
                }
            }
            

            PaintString("Рахунок: " + score, 10, 30);
            if (ship.dead)
            {
                PaintString(Color.Yellow, "F2 нова гра\n" 
                                        + " F10 вихід");
            }
            pic.Image = finalImage;
            //if (DateTime.Now.Second % 2 == 0) GC.Collect();
        }

        private void destroyAsteroid(int index)
        {
            float x = roids[index].x;
            float y = roids[index].y;
            float r = roids[index].r;

            // split the asteroid in two if necessary
            if (r == (float)Math.Ceiling(ROID_SIZE / 2f))
            { // large asteroid
                roids.Add(NewAsteroid(x, y, (float)Math.Ceiling(ROID_SIZE / 4f)));
                roids.Add(NewAsteroid(x, y, (float)Math.Ceiling(ROID_SIZE / 4f)));
                score += ROID_PTS_LGE;
                scoreForLife += ROID_PTS_LGE;
            }
            else if (r == (float)Math.Ceiling(ROID_SIZE / 4f))
            { // medium asteroid
                roids.Add(NewAsteroid(x, y, (float)Math.Ceiling(ROID_SIZE / 8f)));
                roids.Add(NewAsteroid(x, y, (float)Math.Ceiling(ROID_SIZE / 8f)));
                score += ROID_PTS_MED;
                scoreForLife += ROID_PTS_MED;
            }
            else
            {
                score += ROID_PTS_SML;
                scoreForLife += ROID_PTS_SML;
            }

            // Present new ship // by Yozheg
            if (scoreForLife >= 10000) { scoreForLife = scoreForLife - 10000; lives++;  }

            

            // check high score
            //if (score > scoreHigh)
            //{
            //    scoreHigh = score;
            //    localStorage.setItem(SAVE_KEY_SCORE, scoreHigh);
            //}

            // destroy the asteroid
            roids.RemoveAt(index);
            
            // calculate the ratio of remaining asteroids to determine music tempo
            roidsLeft--;
            
            // new level when no more asteroids
            if (roids.Count == 0)
            {
                level++;
                NewLevel();
            }
        }

        private void gameOver()
        {
            ship.dead = true;
        }

        private void explodeShip()
        {
            ship.explodeTime = (int)Math.Ceiling(SHIP_EXPLODE_DUR * FPS);
        }

        private void DrawTheAsteroids()
        {
            // draw the asteroids
            float a, r, x, y; AsteroidStruct tmp;
            for (var i = 0; i < roids.Count; i++)
            {
                tmp = roids[i];
                tmp.a += tmp.rot;
                if(tmp.rot > 0) if(tmp.a == 360) tmp.a = 0; else if(tmp.a == 0) tmp.a = 360;
                
                gfx.DrawImage(RotateImage(asteroid, tmp.a), tmp.x - tmp.r, tmp.y - tmp.r, tmp.r * 2, tmp.r * 2);

                //gfx.DrawArc(new Pen(Color.Green, 1), tmp.x - tmp.r, tmp.y - tmp.r, tmp.r * 2, tmp.r * 2, 0, 360);
                roids[i] = tmp;
            }
        }

        private AsteroidStruct NewAsteroid(float x, float y, float r)
        {
            float lvlMult = 1f + 0.1f * level;
            AsteroidStruct roid = new AsteroidStruct();
            roid.x = x;
            roid.y = y;
            roid.xv = (float)(rndNewAsteroid.NextDouble() * ROID_SPD * lvlMult / FPS * (rndNewAsteroid.NextDouble() < 0.5 ? 1 : -1));
            roid.yv = (float)(rndNewAsteroid.NextDouble() * ROID_SPD * lvlMult / FPS * (rndNewAsteroid.NextDouble() < 0.5 ? 1 : -1));
            roid.a = rndNewAsteroid.Next(0, 360);
            roid.rot = rndNewAsteroid.NextDouble() < 0.5 ? 1 : -1;
            roid.r = r;
            return roid;
        }

        private string ShowSPD()
        {
            return "SPD: " + (Math.Round(Math.Sqrt((ship.thrustX * ship.thrustX) + (ship.thrustY * ship.thrustY)), 2) * 100).ToString();
        }

        private void createAsteroidBelt()
        {
            roids.Clear();
            roidsTotal = (ROID_NUM + level) * 7;
            roidsLeft = roidsTotal;
            Random rnd=new Random();
            float x=0f, y=0f;
            for (var i = 0; i < ROID_NUM + level; i++)
            {
                // random asteroid location (not touching spaceship)
                do
                {
                    x = rnd.Next(SIZE_X);
                    y = rnd.Next(SIZE_Y);
                } while (distBetweenPoints(ship.x, ship.y, x, y) < ROID_SIZE * 2 + ship.r);
                roids.Add(NewAsteroid(x, y, (float)Math.Ceiling((double)ROID_SIZE / 2)));
            }
        }

        private float distBetweenPoints(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }


        private void PaintString(Color c, string msg)
        {
            int w = (int)gfx.MeasureString(msg, new Font("Arial", 16)).Width;
            int ww = (SIZE_X - w) / 2;
            PaintString(c, msg, ww, SIZE_Y / 2);
        }
        private void PaintString(string msg, int x, int y) { PaintString(Color.FromArgb(128, Color.White), msg, x, y); }
        private void PaintString(Color c, string msg, int x, int y)
        {
            gfx.DrawString(msg, new Font("Arial", 16), new SolidBrush(c), x, y, new StringFormat());
        }

        private void HandleEdgeOfScreen()
        {

            if (ship.x < 0 - ship.r) { ship.x = pic.Width + ship.r; }
            else if (ship.x > pic.Width + ship.r) { ship.x = 0 - ship.r; }
            if (ship.y < 0 - ship.r) { ship.y = pic.Height + ship.r; }
            else if (ship.y > pic.Height + ship.r) { ship.y = 0 - ship.r; }
        }

        private void PaintShipThrust()
        {
            ThrustShowFlag = !ThrustShowFlag;
            if (ThrustShowFlag)
            {
                float sinA = (float)Math.Sin(ship.a);
                float cosA = (float)Math.Cos(ship.a);
                ThrustingFigure[0] = new PointF(ship.x - ship.r * (2f / 3f * cosA + 0.2f * sinA), ship.y + ship.r * (2f / 3f * sinA - 0.2f * cosA));
                ThrustingFigure[1] = new PointF(ship.x - ship.r * 3.3f / 3f * cosA, ship.y + ship.r * 3.3f / 3f * sinA);
                ThrustingFigure[2] = new PointF(ship.x - ship.r * (2f / 3f * cosA - 0.2f * sinA), ship.y + ship.r * (2f / 3f * sinA + 0.2f * cosA));
                gfx.DrawLines(new Pen(Color.Orange, 5), ThrustingFigure);
            }
        }

        private void PaintShip()
        {
            float sinA = (float)Math.Sin(ship.a);
            float cosA = (float)Math.Cos(ship.a);
            ShipFigure[0] = new PointF(ship.x + 4f / 3f * ship.r * cosA, ship.y - 4f / 3f * ship.r * sinA);
            ShipFigure[1] = new PointF(ship.x - ship.r * 2f / 3f * (cosA + sinA), ship.y + ship.r * 2f / 3f * (sinA - cosA));
            ShipFigure[2] = new PointF(ship.x - ship.r * 2f / 3f * (cosA - sinA), ship.y + ship.r * 2f / 3f * (sinA + cosA));
            ShipFigure[3] = ShipFigure[0];
            gfx.DrawLines(new Pen(Color.Yellow, 2), ShipFigure);
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            UpdateScene();
        }

        private void shootLaser()
        {
            // create the laser object
            if (ship.canShoot && ship.lasers.Count < LASER_MAX)
            {
                LaserStruct tmp = new LaserStruct();
                tmp.x = ship.x + 4 / 3 * ship.r * (float)Math.Cos(ship.a);
                tmp.y = ship.y - 4 / 3 * ship.r * (float)Math.Sin(ship.a);
                tmp.xv = LASER_SPD * (float)Math.Cos(ship.a) / FPS;
                tmp.yv = -LASER_SPD * (float)Math.Sin(ship.a) / FPS;
                tmp.dist = 0f;
                tmp.explodeTime = 0;
                ship.lasers.Add(tmp);
            }

            // prevent further shooting
            ship.canShoot = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F2:
                    newGame();
                    break;
                case Keys.Space:
                    if(!ship.dead) shootLaser();
                    break;
                case Keys.Left:
                    ship.rot = SHIP_TURN_SPD / 180f * (float)Math.PI / FPS;
                    break;
                case Keys.Right:
                    ship.rot = -SHIP_TURN_SPD / 180f * (float)Math.PI / FPS;
                    break;
                case Keys.Up:
                    ship.thrusting = true;
                    break;
                case Keys.Escape:
                    tmrMain.Enabled = !tmrMain.Enabled;
                    break;
                case Keys.F10:
                    Application.Exit();
                    break;

            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    ship.canShoot = true;
                    break;
                case Keys.Left:
                    ship.rot = 0f;
                    break;
                case Keys.Right:
                    ship.rot = 0f;
                    break;
                case Keys.Up:
                    ship.thrusting = false;
                    break;
            }
        }
    }
}
