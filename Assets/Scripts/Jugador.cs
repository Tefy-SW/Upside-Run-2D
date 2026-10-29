using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Jugador : MonoBehaviour
{
    public Animator animador;
    public Font fuentePixel;
    public GameManager gameManager;
    [Header("Movimiento")]
    [Tooltip("Impulso hacia arriba. Más alto = salto más alto.")]
    public float fuerzaSalto = 11f;
    [Range(.5f, 2f), Tooltip("1 = ritmo que guardaste en correr; 1.25 = 25% más rápido.")]
    public float velocidadAnimacion = 1f;
    [Header("Cassette: levitación temporal")]
    [Min(.5f)] public float duracionLevitacion = 6f;
    [Min(.5f)] public float alturaLevitacion = 3.4f;
    [Min(.5f)] public float velocidadSubida = 4f;
    public bool estaLevitando { get { return tiempoRestante > 0; } }
    public float tiempoLevitacion { get { return tiempoRestante; } }
    float tiempoRestante, gravedadNormal, alturaObjetivo;

    public int vida = 3;
    [Header("Protección después de un golpe")]
    [Min(.1f)] public float duracionProteccion = 1.25f;
    [Min(.1f)] public float duracionHerido = .38f;
    [HideInInspector] public float tiempoProteccion;
    public bool enSuelo { get; private set; }
    public bool agachado { get; private set; }
    public bool sinVida { get { return vida <= 0; } }
    public int energia { get; private set; }
    [Min(1)] public int vidaMaxima = 3;
    [Min(1)] public int capacidadEnergia = 20;
    public int energiaBarra { get { return Mathf.Min(energia, capacidadEnergia); } }
    public Rigidbody2D cuerpo { get; private set; }
    public CapsuleCollider2D colision { get; private set; }
    string animacionActual;
    float tiempoHerido, tiempoAgachado;
    bool saltoPedido, teclaAgacharse;
    bool posturaPreparada;
    private RaycastHit2D[] contactos = new RaycastHit2D[8];
    void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>(); colision = GetComponent<CapsuleCollider2D>();
        gravedadNormal = cuerpo.gravityScale;
        CambiarAgachado(false); CambiarAnimacion("Run");
    }
    void Update()
    {
        if (!gameManager.partidaIniciada) return;
        
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
            if (k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) PedirSalto();
            teclaAgacharse = k.downArrowKey.isPressed || k.sKey.isPressed;
        }
        tiempoProteccion = Mathf.Max(0, tiempoProteccion - Time.deltaTime);
        tiempoHerido = Mathf.Max(0, tiempoHerido - Time.deltaTime);
        tiempoAgachado = Mathf.Max(0, tiempoAgachado - Time.deltaTime);
        if (sinVida) { TerminarLevitacion(); CambiarAnimacion("Death"); return; }
        if (tiempoRestante > 0)
        {
            tiempoRestante = Mathf.Max(0, tiempoRestante - Time.deltaTime);
            if (tiempoRestante == 0) TerminarLevitacion();
        }
        CambiarAgachado(!estaLevitando && enSuelo && (teclaAgacharse || tiempoAgachado > 0));
        if (tiempoHerido > 0) CambiarAnimacion("Hurt");
        else if (estaLevitando) CambiarAnimacion("Levitate");
        else if (!enSuelo) CambiarAnimacion("Jump");
        else if (agachado) CambiarAnimacion("Slide");
        else CambiarAnimacion("Run");
    }
    void FixedUpdate()
    {
        if (!gameManager.partidaIniciada) return;
        if (estaLevitando && !sinVida)
        {
            cuerpo.gravityScale = 0;
            cuerpo.linearVelocity = Vector2.zero;
            float y = Mathf.MoveTowards(cuerpo.position.y, alturaObjetivo, velocidadSubida * Time.fixedDeltaTime);
            cuerpo.MovePosition(new Vector2(cuerpo.position.x, y));
            enSuelo = false; saltoPedido = false;
            return;
        }
        // Revisar el suelo justo debajo del jugador, incluso al agacharse.
        ContactFilter2D filtro = ContactFilter2D.noFilter;
        filtro.useTriggers = false;
        int count = colision.Cast(Vector2.down, filtro, contactos, .08f);
        enSuelo = false;
        for (int i = 0; i < count; i++) if (contactos[i].normal.y > .5f && cuerpo.linearVelocity.y <= .2f) enSuelo = true;
        if (!sinVida && saltoPedido && enSuelo)
        {
            CambiarAgachado(false); tiempoAgachado = 0; cuerpo.linearVelocity = new Vector2(0, fuerzaSalto);
            enSuelo = false;
        }
        saltoPedido = false;
    }
    public void PedirSalto() { saltoPedido = true; }
    public void Agacharse(float duracion = .8f) { tiempoAgachado = duracion; }
    public void EmpezarLevitacion()
    {
        if (sinVida) return;
        
        float alturaPersonaje = animador.GetComponent<SpriteRenderer>().bounds.size.y;
        float techo = Camera.main.transform.position.y + Camera.main.orthographicSize;
        alturaObjetivo = Mathf.Min(gameManager.alturaSuelo + alturaLevitacion, techo - alturaPersonaje - .45f);
        tiempoRestante = duracionLevitacion;
        gameManager.CrearGemasDelCielo(alturaObjetivo);
        cuerpo.gravityScale = 0; cuerpo.linearVelocity = Vector2.zero;
        CambiarAgachado(false); saltoPedido = false;
    }
    void TerminarLevitacion()
    {
        tiempoRestante = 0;
        if (cuerpo != null) cuerpo.gravityScale = gravedadNormal;
    }
    void CambiarAgachado(bool valor)
    {
        if (posturaPreparada && agachado == valor) return;
        posturaPreparada = true;
        agachado = valor;
        colision.size = valor ? new Vector2(.68f, .72f) : new Vector2(.68f, 1.62f);
        colision.offset = new Vector2(0, colision.size.y / 2 + .02f);
    }
    void CambiarAnimacion(string next)
    {
        if (animador == null) return;
        animador.speed = next == "Run" ? Mathf.Max(.1f, velocidadAnimacion) : 1;
        if (animacionActual == next) return;
        animacionActual = next; animador.Play(next, 0, 0);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameManager.partidaIniciada || sinVida) return;
        var item = other.GetComponent<Objeto>();
        if (item == null) return;
        if (item.tipo == Objeto.Tipo.Gema)
        {
            if (item.recogido) return;
            item.recogido = true; energia++; item.gameObject.SetActive(false);
        }
        else if (item.tipo == Objeto.Tipo.Cassette)
        {
            if (item.recogido) return;
            item.recogido = true; item.gameObject.SetActive(false); EmpezarLevitacion();
        }
        else if (tiempoProteccion <= 0)
        {
            vida--; tiempoProteccion = duracionProteccion; tiempoHerido = duracionHerido;
            if (sinVida) gameManager.TerminarPartida();
        }
    }
    GUIStyle titleStyle, textStyle;
    static void Panel(Rect r, Color color)
    {
        Color old = GUI.color; GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }
    void OnGUI()
    {
        if (!gameManager.partidaIniciada || gameManager.partidaTerminada) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { font = fuentePixel, fontSize = 16 };
            textStyle = new GUIStyle(GUI.skin.label) { font = fuentePixel, fontSize = 10 };
        }
        Matrix4x4 old = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Panel(new Rect(16,16,350,159),new Color(.025f,.04f,.08f,.93f));
        GUI.Label(new Rect(30,23,310,28), "UPSIDE RUN", titleStyle);
        string condition = vida >= 3 ? "Bien" : vida == 2 ? "Herido" : vida == 1 ? "Crítico" : "Sin vida";
        GUI.Label(new Rect(30,53,310,24), "VIDA  " + Mathf.Max(0,vida) + "/" + vidaMaxima + "  ·  " + condition, textStyle);
        Panel(new Rect(30,80,316,18),new Color(.16f,.12f,.17f));
        Panel(new Rect(32,82,312*Mathf.Clamp01(vida/(float)vidaMaxima),14),vida>1?new Color(.92f,.22f,.31f):new Color(1,.52f,.15f));
        GUI.Label(new Rect(30,103,310,24),"ENERGÍA  " + energiaBarra + "/" + capacidadEnergia + "   Total: " + energia, textStyle);
        Panel(new Rect(30,132,316,18),new Color(.09f,.18f,.22f));
        Panel(new Rect(32,134,312*energiaBarra/capacidadEnergia,14),new Color(.17f,.88f,.94f));
        GUI.Label(new Rect(386,25,420,26),"DISTANCIA  " + Mathf.FloorToInt(gameManager.distancia) + " m",titleStyle);
        Panel(new Rect(16,672,620,32),new Color(.025f,.04f,.08f,.85f));
        GUI.Label(new Rect(28,677,605,26),"Espacio / ↑: saltar    ↓ / S: deslizar    R: inicio",textStyle);
        if (estaLevitando)
        {
            Panel(new Rect(386,62,400,48),new Color(.025f,.04f,.08f,.9f));
            GUI.Label(new Rect(400,71,375,30),"CASSETTE · LEVITANDO  " + tiempoRestante.ToString("0.0") + " s",textStyle);
        }
        GUI.matrix=old;
    }
}
