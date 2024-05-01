using Raylib_cs;
using System.Collections.Generic;
using System;
using System.Numerics;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection.Emit;
using System.Collections;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using Color = Raylib_cs.Color;
using Image = Raylib_cs.Image;
using Rectangle = Raylib_cs.Rectangle;


class Program
{
    static bool paused = false;
    static bool gameOver = false;
    static bool gameOverScreenOn = false;
    static bool soundOn = true;

    public static void Main()
    {
        Raylib.InitWindow(800, 480, "Game 01");

        // Texture
        Image perlinNoise = Raylib.GenImagePerlinNoise(800, 480, 50, 50, 4.0f);
        Texture2D texture = Raylib.LoadTextureFromImage(perlinNoise);

        // Sounds
        Raylib.InitAudioDevice();

        Sound backgroundSound = Raylib.LoadSound("Atmosphere-05.wav");
        Sound bulletSound = Raylib.LoadSound("bullet.wav");
        Sound gameNewSound = Raylib.LoadSound("Oneshot-09.wav");

        Raylib.SetTargetFPS(60);

        Game game = new Game();
        GameOverScreen gameOverScreen = new GameOverScreen();

        while (!Raylib.WindowShouldClose())
        {
            gameOver = game.GameOver;

            // Pause
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER))
            {
                paused = !paused;
            }
            if (!paused) game.Update();

            // Sound
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_T))
            {
                soundOn = !soundOn;
                game.SoundOn = soundOn;
            }

            // Game Over
            if (gameOver)
            {
                gameOverScreenOn = true;
                gameOverScreen.GameOverScreenOn = true;
                gameOverScreen.Update();
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_P))
                {
                    game.GameOver = false;
                    gameOver = false;
                    gameOverScreenOn = false;
                    gameOverScreen.GameOverScreenOn = false;
                    game.Lifes = game.LifesRestart;
                    game.Score = game.ScoreRestart;
                    gameOverScreen.Update();
                    if (soundOn) Raylib.PlaySound(gameNewSound);
                }
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);

            Raylib.DrawTexture(texture, 0, 0, Color.DARKBLUE);

            // Background Sound On/Off
            if (soundOn && !Raylib.IsSoundPlaying(backgroundSound)) Raylib.PlaySound(backgroundSound);
            if (!soundOn) Raylib.StopSound(backgroundSound);

            game.Draw();

            // Game information
            Raylib.DrawText($"Score: {game.Score}", 30, 30, 30, Color.WHITE);
            Raylib.DrawText($"Lifes: {game.Lifes}", 30, 70, 30, Color.WHITE);
            Raylib.DrawText("Sound On/Off - Press T", 20, 450, 20, Color.WHITE);
            if (paused) Raylib.DrawText("Pause", 290, 200, 70, Color.MAROON);

            if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE) && soundOn) Raylib.PlaySound(bulletSound);

            gameOverScreen.Draw();

            Raylib.EndDrawing();
        }

        Raylib.UnloadTexture(texture);
        Raylib.UnloadSound(backgroundSound);
        Raylib.UnloadSound(bulletSound);
        Raylib.UnloadSound(gameNewSound);

        Raylib.CloseAudioDevice();

        game.Unsubscribe();

        Raylib.CloseWindow();
    }
}

public class Game
{
    List<Bullet> bullets = new List<Bullet>(32);
    List<Enemy> enemies = new List<Enemy>();
    Player player;
    float enemyTimer;
    float gameTimer;
    private int score = 0;
    private int scoreRestart = 0;
    private int lifes = 3;
    private int lifesRestart = 3;
    private bool gameOver = false;
    private bool soundOn = true;

    // Score
    public int Score
    {
        set { score = value; }
        get { return score; }
    }
    public int ScoreRestart
    { get { return scoreRestart; } }

    // Lifes
    public int Lifes
    {
        set { lifes = value; }
        get { return lifes; }
    }
    public int LifesRestart
    { get { return lifesRestart; } }

    // Game Over
    public bool GameOver
    {
        set { gameOver = value; }
        get { return gameOver; }
    }
    public bool SoundOn
    {
        set { soundOn = value; }
    }

    public Game()
    {
        player = new Player(bullets);
        player.BulletSpawned += OnBulletSpawned;
        Bullet.LeftScreen += OnLeftScreen;
    }

    public void Unsubscribe()
    {
        Bullet.LeftScreen -= OnLeftScreen;
        player.BulletSpawned -= OnBulletSpawned;
    }

    public void OnLeftScreen(Bullet bullet)
    {
        bullets.Remove(bullet);
    }

    public void OnBulletSpawned(float x, float y, Vector2 dir)
    {
        Bullet b = new Bullet(x, y, dir);
        bullets.Add(b);
    }

    public void Update()
    {
        // Game Over condition
        if (lifes == 0)
        {
            gameOver = true;
            enemies.Clear();
            player.PosX = 400f;
            player.PosY = 240f;
            return;
        }
        enemyTimer += Raylib.GetFrameTime();
        if (enemyTimer >= 3f)
        {
            enemyTimer = 0f;
            int randX = Raylib.GetRandomValue(0, 800);
            Enemy enemy = new Enemy(randX, -50);
            enemies.Add(enemy);
        }

        // A lot of enemies at the same time mode
        gameTimer += Raylib.GetFrameTime();
        if (gameTimer >= 10f)
        {
            gameTimer = 0f;
            for (int c = 11; c > 0; c--)
            {
                int randX = Raylib.GetRandomValue(0, 800);
                Enemy enemy = new Enemy(randX, Raylib.GetRandomValue(-550, -20));
                enemies.Add(enemy);
            }
        }


        // Collision check
        // genestete for-Schleife fuer Enemies u. Bullets
        for (int b = bullets.Count - 1; b >= 0; b--)
        {
            for (int e = enemies.Count - 1; e >= 0; e--)
            {
                // Vector2 jeweils aus posX u. posY erstellen
                Vector2 bPos = bullets[b].Pos;
                Vector2 ePos = enemies[e].Pos;
                float bRad = bullets[b].Radius;
                float eRad = enemies[e].Radius;
                // Raylib.CheckCollisionCircles()-Methode benutzen
                // wenn sie kollidieren: aus den Listen rauswerfen
                if (Raylib.CheckCollisionCircles(bPos, bRad, ePos, eRad))
                {
                    bullets.Remove(bullets[b]);
                    enemies.Remove(enemies[e]);
                    score += 10;
                    Sound shootedSound = Raylib.LoadSound("bullet_impact_metal_heavy_08.wav");
                    if (soundOn) Raylib.PlaySound(shootedSound);
                    break;
                }
            }
        }

        // Check collision between the player and enemies
        for (int e = enemies.Count - 1; e >= 0; e--)
        {
            Vector2 pPos = player.Pos;
            Vector2 ePos = enemies[e].Pos;
            float pRad = player.Radius;
            float eRad = enemies[e].Radius;
            if (Raylib.CheckCollisionCircles(pPos, pRad, ePos, eRad))
            {
                enemies.Remove(enemies[e]);
                lifes -= 1;
                Sound collisionSound = Raylib.LoadSound("beep-3.wav");
                if (soundOn) Raylib.PlaySound(collisionSound);
                break;
            }
        }

        player.Update();
        for (int i = 0; i < bullets.Count; i++)
        {
            bullets[i].Update();
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].Update();
        }
    }
    public void Draw()
    {
        player.Draw();
        for (int i = 0; i < bullets.Count; i++)
        {
            bullets[i].Draw();
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].Draw();
        }
    }
}

public class Player
{
    List<Bullet> bullets = new List<Bullet>();
    public int sizeX = 15;
    public int sizeY = 30;
    float posX = 400f;
    float posY = 240f;
    float angle;
    Vector2 startDir = new Vector2(0f, -1f);
    Vector2 dir = new Vector2(0f, -1f);
    float speed;


    // For checking player-enemy collision
    public Vector2 Pos => new Vector2(posX, posY);
    public float Radius => sizeX;


    public float PosX
    {
        set { posX = value; }
        get { return posX; }
    }
    public float PosY
    {
        set { posY = value; }
        get { return posY; }
    }


    public event Action<float, float, Vector2>? BulletSpawned;

    public Player(List<Bullet> bullets)
    {
        this.bullets = bullets;
    }

    public void Update()
    {
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W) || Raylib.IsKeyDown(KeyboardKey.KEY_UP))
        {
            posX += dir.X;
            posY += dir.Y;
            if (posY < 0) posY = 0;

        }
        if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
        {
            speed = -3f;
            angle -= 3f;
            dir = Raymath.Vector2Rotate(startDir, Raylib.DEG2RAD * angle);
        }
        else if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
        {
            angle += 3f;
            dir = Raymath.Vector2Rotate(startDir, Raylib.DEG2RAD * angle);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            BulletSpawned?.Invoke(posX, posY, dir);
        }
    }

    public void Draw()
    {
        Rectangle rect = new Rectangle(posX, posY, sizeX, sizeY);
        Vector2 origin = new Vector2(sizeX / 2, sizeY / 2);
        Raylib.DrawRectanglePro(rect, origin, angle, Color.SKYBLUE);
        Raylib.DrawCircle((int)posX, (int)posY, 3f, Color.RED);

    }
}

public class Bullet
{
    public Vector2 Pos => new Vector2(posX, posY);
    float posX;
    float posY;
    Vector2 dir;
    float speed = 5;

    public float Radius => radius;
    float radius = 5f;

    public bool isAlive = true;

    public static event Action<Bullet>? LeftScreen;

    public Bullet(float x, float y, Vector2 dir)
    {
        posX = x;
        posY = y;
        this.dir = dir;
    }

    public void Update()
    {
        posX += dir.X * speed;
        posY += dir.Y * speed;

        if (posY < 0 || posY > 480 || posX < 0 || posX > 800)
        {
            LeftScreen?.Invoke(this);
        }
    }

    public void Draw()
    {
        Raylib.DrawCircle((int)posX, (int)posY, radius, Color.RED);
    }
}

public class Enemy
{
    public float Radius => sizeX / 2;
    public int sizeX = 20;
    public int sizeY = 20;

    public Vector2 Pos => new Vector2(posX, posY);
    int posX;
    int posY;

    public Enemy(int x, int y)
    {
        posX = x;
        posY = y;
    }

    public void Update()
    {
        posY += 3;
    }

    public void Draw()
    {
        Raylib.DrawRectangle(posX - sizeX / 2, posY - sizeY / 2, sizeX, sizeY, Color.GREEN);
    }
}

// Game Over screen
public class GameOverScreen
{
    private int gameOverScreenX = 0;
    private int gameOverScreenY = 0;
    private int gameOverPosX = 900;
    private bool gameOverScreenOn = false;

    public bool GameOverScreenOn
    {
        set { gameOverScreenOn = value; }
        get { return gameOverScreenOn; }
    }

    public void Update()
    {
        if (gameOverScreenOn)
        {
            gameOverScreenX = 800;
            gameOverScreenY = 480;
            gameOverPosX = 140;
        }
        else
        {
            gameOverScreenX = 0;
            gameOverScreenY = 0;
            gameOverPosX = 900;
        }
    }

    public void Draw()
    {
        Raylib.DrawRectangle(0, 0, gameOverScreenX, gameOverScreenY, Color.DARKBLUE);
        Raylib.DrawText("Game Over", gameOverPosX, 150, 100, Color.WHITE);
        Raylib.DrawText("Press P for a new game", gameOverPosX, 290, 40, Color.RAYWHITE);
    }

}





