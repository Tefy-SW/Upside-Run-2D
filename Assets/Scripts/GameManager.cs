using System.Collections.Generic;
using UnityEngine;

// Prepara objetos al iniciar y los reutiliza durante la partida.
public class GameManager : MonoBehaviour
{
    [Header("Recursos")]
    public GameObject troncoPrefab;
    public GameObject murcielagoPrefab;
    public Sprite gemaSprite;
    public Sprite cassetteSprite;
    public Material materialSprites;
    public Jugador jugador;

    [Header("Movimiento del escenario")]
    [Min(1)] public float velocidad = 4f;
    public float alturaSuelo = -2.8f;

    [Header("Obstáculos: segundos entre apariciones")]
    [Min(0)] public float esperaPrimerObstaculo = 2.5f;
    [Min(1)] public float intervaloMinimoObstaculos = 2.4f;
    [Min(1)] public float intervaloMaximoObstaculos = 4.2f;
    public float alturaMurcielago = 1.7f;

    [Header("Gemas y cassette")]
    [Min(1)] public float intervaloGemas = 2f;
    [Min(0)] public float esperaPrimerCassette = 4f;
    [Min(5)] public float intervaloMinimoCassette = 18f;
    [Min(5)] public float intervaloMaximoCassette = 25f;
    [Range(1, 20)] public int cantidadGemasCielo = 9;
    [Min(.2f)] public float segundosEntreGemasCielo = .4f;

    [Header("Música")]
    [InspectorName("Musica Ambiental")] public AudioClip musica;
    public AudioClip musicaLevitacion;
    [Min(0), Tooltip("Segundo desde el que empieza el tramo de levitación.")]
    public float inicioMusicaLevitacion = 0;
    [Min(0), Tooltip("Segundo en el que termina el tramo. 0 = final de la canción.")]
    public float finMusicaLevitacion = 0;
    [Range(0, 1)] public float volumenMusica = .2f;
    [Range(0, 1)] public float volumenLevitacion = .25f;

    [Header("Object pooling: cantidades iniciales")]
    [Min(1)] public int obstaculosPorTipo = 6;
    [Min(1)] public int gemasEnPool = 32;
    [Min(1)] public int cassettesEnPool = 3;

    public float distancia;
    public bool partidaTerminada;
    [HideInInspector] public bool partidaIniciada;
    private List<GameObject> objetos = new List<GameObject>();
    private Paisaje[] paisajes;
    private AudioSource sonido;
    private AudioSource sonidoLevitacion;
    private bool musicaDeVuelo;
    private List<GameObject> troncos = new List<GameObject>();
    private List<GameObject> murcielagos = new List<GameObject>();
    private List<GameObject> gemas = new List<GameObject>();
    private List<GameObject> cassettes = new List<GameObject>();
    private float tiempoObstaculo, tiempoGemas, tiempoCassette;
    private float inicioX, salidaX;
    private int ultimoTipo = -1, repeticiones;

    void Start()
    {
        Camera camara = Camera.main;
        inicioX = camara.ViewportToWorldPoint(new Vector3(1.15f, .5f, 10)).x;
        salidaX = camara.ViewportToWorldPoint(new Vector3(0, .5f, 10)).x - 3;
        paisajes = FindObjectsByType<Paisaje>();
        tiempoObstaculo = esperaPrimerObstaculo;
        tiempoGemas = .5f;
        tiempoCassette = esperaPrimerCassette;
        sonido = GetComponent<AudioSource>();
        if (sonido == null) sonido = gameObject.AddComponent<AudioSource>();
        sonido.clip = musica;
        sonido.loop = true;
        sonido.playOnAwake = false;
        sonido.spatialBlend = 0;

        sonidoLevitacion = gameObject.AddComponent<AudioSource>();
        sonidoLevitacion.playOnAwake = false;
        sonidoLevitacion.loop = false;
        sonidoLevitacion.spatialBlend = 0;
        PrepararPool();
        // El recorrido espera al botón JUGAR.
        jugador.cuerpo.simulated = false;
        jugador.animador.enabled = false;
        foreach (Paisaje paisaje in paisajes) paisaje.velocidad = 0;
    }

    void Update()
    {
        sonido.volume = volumenMusica;
        sonidoLevitacion.volume = volumenLevitacion;
        if (!partidaIniciada || partidaTerminada) return;
        ActualizarMusica();
        distancia += velocidad * Time.deltaTime;
        foreach (Paisaje paisaje in paisajes)
        {
            if (paisaje.name.Contains("Ground")) paisaje.velocidad = velocidad;
            else if (paisaje.name.Contains("Forest")) paisaje.velocidad = velocidad * .16f;
            else paisaje.velocidad = velocidad * .03f;
        }

        tiempoObstaculo -= Time.deltaTime;
        tiempoGemas -= Time.deltaTime;
        tiempoCassette -= Time.deltaTime;
        if (tiempoCassette <= 0 && cassetteSprite != null)
        {
            CrearRecogible("Cassette", cassetteSprite, new Vector2(inicioX, alturaSuelo + .85f), Objeto.Tipo.Cassette, .8f);
            tiempoCassette = Random.Range(intervaloMinimoCassette, Mathf.Max(intervaloMinimoCassette, intervaloMaximoCassette));
            tiempoObstaculo = Mathf.Max(tiempoObstaculo, 1.8f);
        }
        if (tiempoObstaculo <= 0) CrearObstaculo();
        if (tiempoGemas <= 0)
        {
           
            if (!jugador.estaLevitando)
                for (int i = 0; i < 3; i++)
                    CrearRecogible("Gema", gemaSprite, new Vector2(inicioX + i * 1.1f, alturaSuelo + .85f), Objeto.Tipo.Gema, .65f);
            tiempoGemas = intervaloGemas;
        }
        MoverYLimpiar();
    }

    public void ComenzarPartida()
    {
        if (partidaIniciada) return;
        partidaIniciada = true;
        jugador.cuerpo.simulated = true;
        jugador.animador.enabled = true;
        if (musica != null) sonido.Play();
    }

    void CrearObstaculo()
    {
        int tipo = Random.Range(0, 2);
        if (tipo == ultimoTipo && repeticiones >= 2) tipo = 1 - tipo;
        if (tipo == ultimoTipo) repeticiones++;
        else repeticiones = 1;
        ultimoTipo = tipo;

        GameObject prefab = tipo == 0 ? troncoPrefab : murcielagoPrefab;
        float altura = tipo == 0 ? alturaSuelo : alturaSuelo + alturaMurcielago;
        List<GameObject> lista = tipo == 0 ? troncos : murcielagos;
        GameObject obstaculo = BuscarLibre(lista);
        if (obstaculo == null) obstaculo = PrepararObstaculo(lista, prefab);
        Activar(obstaculo, new Vector2(inicioX, altura));
        tiempoObstaculo = Random.Range(intervaloMinimoObstaculos, Mathf.Max(intervaloMinimoObstaculos, intervaloMaximoObstaculos));
    }

    GameObject BuscarLibre(List<GameObject> lista)
    {
        foreach (GameObject objeto in lista)
            if (!objeto.activeSelf) return objeto;
        return null;
    }

    void PrepararPool()
    {
        for (int i = 0; i < obstaculosPorTipo; i++)
        {
            PrepararObstaculo(troncos, troncoPrefab);
            PrepararObstaculo(murcielagos, murcielagoPrefab);
        }
        for (int i = 0; i < gemasEnPool; i++)
            PrepararRecogible(gemas, "Gema", gemaSprite, Objeto.Tipo.Gema, .65f);
        for (int i = 0; i < cassettesEnPool; i++)
            PrepararRecogible(cassettes, "Cassette", cassetteSprite, Objeto.Tipo.Cassette, .8f);
    }

    GameObject PrepararObstaculo(List<GameObject> lista, GameObject prefab)
    {
        GameObject nuevo = Instantiate(prefab, transform);
        Objeto objeto = nuevo.GetComponent<Objeto>();
        if (objeto == null) objeto = nuevo.AddComponent<Objeto>();
        objeto.tipo = Objeto.Tipo.Obstaculo;
        foreach (Collider2D colision in nuevo.GetComponents<Collider2D>()) colision.isTrigger = true;
        nuevo.SetActive(false);
        lista.Add(nuevo);
        objetos.Add(nuevo);
        return nuevo;
    }

    GameObject PrepararRecogible(List<GameObject> lista, string nombre, Sprite sprite, Objeto.Tipo tipo, float ancho)
    {
        if (sprite == null) return null;
        GameObject nuevo = new GameObject(nombre);
        nuevo.transform.SetParent(transform);
        SpriteRenderer imagen = nuevo.AddComponent<SpriteRenderer>();
        imagen.sprite = sprite;
        imagen.sortingOrder = 30;
        if (materialSprites != null) imagen.sharedMaterial = materialSprites;
        float escala = ancho / sprite.bounds.size.x;
        nuevo.transform.localScale = Vector3.one * escala;
        CircleCollider2D colision = nuevo.AddComponent<CircleCollider2D>();
        colision.isTrigger = true;
        colision.radius = .3f / escala;
        nuevo.AddComponent<Objeto>().tipo = tipo;
        nuevo.SetActive(false);
        lista.Add(nuevo);
        objetos.Add(nuevo);
        return nuevo;
    }

    void Activar(GameObject objeto, Vector2 posicion)
    {
        objeto.transform.position = posicion;
        objeto.GetComponent<Objeto>().recogido = false;
        objeto.SetActive(true);
    }

    void CrearRecogible(string nombre, Sprite sprite, Vector2 posicion, Objeto.Tipo tipo, float ancho)
    {
        if (sprite == null) return;
        List<GameObject> lista = tipo == Objeto.Tipo.Gema ? gemas : cassettes;
        GameObject objeto = BuscarLibre(lista);
        // Si se aumentan mucho las apariciones, el pool puede crecer cuando haga falta.
        if (objeto == null) objeto = PrepararRecogible(lista, nombre, sprite, tipo, ancho);
        objeto.name = nombre;
        Activar(objeto, posicion);
    }

    void ActualizarMusica()
    {
        bool usarMusicaVuelo = jugador.estaLevitando && musicaLevitacion != null;
        if (usarMusicaVuelo != musicaDeVuelo)
        {
            musicaDeVuelo = usarMusicaVuelo;
            if (musicaDeVuelo)
            {
                sonido.Pause();
                sonidoLevitacion.clip = musicaLevitacion;
                sonidoLevitacion.Play();
                sonidoLevitacion.time = Mathf.Clamp(inicioMusicaLevitacion, 0, Mathf.Max(0, musicaLevitacion.length - .01f));
            }
            else
            {
                sonidoLevitacion.Stop();
                sonido.UnPause();
            }
        }
        if (!musicaDeVuelo) return;
        float inicio = Mathf.Clamp(inicioMusicaLevitacion, 0, Mathf.Max(0, musicaLevitacion.length - .01f));
        float fin = finMusicaLevitacion > inicio ? Mathf.Min(finMusicaLevitacion, musicaLevitacion.length) : musicaLevitacion.length;
        // Repetir solo el tramo elegido, aunque la canción sea más larga.
        if (sonidoLevitacion.time >= fin || !sonidoLevitacion.isPlaying)
        {
            sonidoLevitacion.Play();
            sonidoLevitacion.time = inicio;
        }
    }

    public void CrearGemasDelCielo(float alturaPies)
    {
        // Empiezan después de la subida y terminan antes de que acabe el cassette.
        float subida = Mathf.Abs(alturaPies - jugador.transform.position.y) / jugador.velocidadSubida;
        float primera = subida + .6f;
        for (int i = 0; i < cantidadGemasCielo; i++)
        {
            float llegada = primera + i * segundosEntreGemasCielo;
            if (llegada > jugador.duracionLevitacion - .7f) break;
            float x = jugador.transform.position.x + velocidad * llegada;
            CrearRecogible("Gema del cielo", gemaSprite, new Vector2(x, alturaPies + .85f), Objeto.Tipo.Gema, .65f);
        }
    }

    void MoverYLimpiar()
    {
        foreach (GameObject objeto in objetos)
        {
            if (!objeto.activeSelf) continue;
            objeto.transform.position += Vector3.left * velocidad * Time.deltaTime;
            if (objeto.transform.position.x < salidaX)
                objeto.SetActive(false); // Regresa al pool; no se destruye.
        }
    }

    public void TerminarPartida()
    {
        partidaTerminada = true;
        foreach (Paisaje paisaje in paisajes) paisaje.velocidad = 0;
        sonido.Stop();
        sonidoLevitacion.Stop();
    }
}
