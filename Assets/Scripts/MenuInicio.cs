using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuInicio : MonoBehaviour
{
    public GameManager gameManager;
    public Jugador jugador;
    public Font fuentePixel;
    public Texture2D portada;
    public string bienvenida = "BIENVENIDOS AL MUNDO DEL REVÉS";
    [Range(48, 100)] public int tamanoTitulo = 72;
    [Range(48, 100)] public int tamanoGameOver = 72;
    private GUIStyle titulo, subtitulo, pequeno, boton;

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null && !gameManager.partidaIniciada && teclado.enterKey.wasPressedThisFrame)
            gameManager.ComenzarPartida();
    }

    void OnGUI()
    {
        if (gameManager.partidaIniciada && !gameManager.partidaTerminada) return;
        if (titulo == null)
        {
            titulo = new GUIStyle(GUI.skin.label);
            titulo.font = fuentePixel;
            titulo.alignment = TextAnchor.MiddleCenter;
            subtitulo = new GUIStyle(titulo); subtitulo.fontSize = 16;
            pequeno = new GUIStyle(titulo); pequeno.fontSize = 8;
            boton = new GUIStyle(titulo); boton.fontSize = 24;
            boton.normal.textColor = Color.white;
            boton.hover.textColor = new Color(.5f, 1, 1);
            boton.active.textColor = Color.white;
        }
        Matrix4x4 anterior = GUI.matrix;
        float escala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * escala) / 2,
            (Screen.height - 720 * escala) / 2, 0), Quaternion.identity, new Vector3(escala, escala, 1));
        if (!gameManager.partidaIniciada) DibujarPortada();
        else DibujarGameOver();
        GUI.matrix = anterior;
    }

    void DibujarPortada()
    {
        if (portada != null) GUI.DrawTexture(new Rect(0, 0, 1280, 720), portada, ScaleMode.ScaleAndCrop);
        Panel(new Rect(0, 0, 1280, 216), new Color(.005f, .01f, .03f, .35f));
        pequeno.normal.textColor = new Color(.5f, .9f, 1);
        GUI.Label(new Rect(120, 20, 1040, 24), "CINCO AMIGOS. UN MUNDO AL REVÉS.", pequeno);
        titulo.fontSize = tamanoTitulo;
        titulo.normal.textColor = new Color(.015f, .1f, .19f);
        GUI.Label(new Rect(66, 54, 1160, 110), "UPSIDE RUN", titulo);
        titulo.normal.textColor = new Color(1, .15f, .25f);
        GUI.Label(new Rect(60, 48, 1160, 110), "UPSIDE RUN", titulo);
        subtitulo.normal.textColor = new Color(.9f, .97f, 1);
        GUI.Label(new Rect(100, 170, 1080, 36), bienvenida, subtitulo);

        Panel(new Rect(0, 570, 1280, 150), new Color(.008f, .02f, .045f, .95f));
        Panel(new Rect(100, 570, 1080, 3), new Color(.2f, .7f, .8f));
        if (BotonPixel(new Rect(460, 590, 360, 60), "JUGAR")) gameManager.ComenzarPartida();
        pequeno.normal.textColor = new Color(.65f, .8f, .9f);
        GUI.Label(new Rect(90, 660, 1100, 24), "FINN DISPONIBLE · MÁS PERSONAJES PRÓXIMAMENTE", pequeno);
        GUI.Label(new Rect(90, 692, 1100, 20), "ENTER: JUGAR     ESPACIO: SALTAR     S / FLECHA ABAJO: AGACHARSE", pequeno);
    }

    void DibujarGameOver()
    {
        Panel(new Rect(0, 0, 1280, 720), new Color(.005f, .01f, .03f, .35f));
        titulo.fontSize = tamanoGameOver;
        titulo.normal.textColor = new Color(1, .15f, .25f);
        GUI.Label(new Rect(60, 170, 1160, 135), "GAME OVER", titulo);
        subtitulo.normal.textColor = Color.white;
        GUI.Label(new Rect(120, 325, 1040, 48), "EL MUNDO DEL REVÉS TE ATRAPÓ", subtitulo);
        pequeno.fontSize = 16;
        pequeno.normal.textColor = new Color(.4f, .9f, 1);
        GUI.Label(new Rect(120, 400, 1040, 40), "DISTANCIA " + Mathf.FloorToInt(gameManager.distancia)
            + " m   ENERGÍA " + jugador.energia, pequeno);
        boton.fontSize = 16;
        if (BotonPixel(new Rect(400, 486, 480, 64), "VOLVER AL INICIO")) VolverAlInicio();
        pequeno.fontSize = 8;
        pequeno.normal.textColor = Color.white;
        GUI.Label(new Rect(160, 590, 960, 28), "PULSA R PARA VOLVER AL INICIO", pequeno);
    }

    bool BotonPixel(Rect zona, string texto)
    {
        Panel(new Rect(zona.x + 5, zona.y + 5, zona.width, zona.height), new Color(.015f, .005f, .02f));
        Panel(zona, new Color(1, .25f, .4f));
        Panel(new Rect(zona.x + 3, zona.y + 3, zona.width - 6, zona.height - 6), new Color(.4f, .02f, .1f));
        return GUI.Button(zona, texto, boton);
    }

    public void VolverAlInicio()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    void Panel(Rect zona, Color color)
    {
        Color anterior = GUI.color; GUI.color = color;
        GUI.DrawTexture(zona, Texture2D.whiteTexture); GUI.color = anterior;
    }
}
