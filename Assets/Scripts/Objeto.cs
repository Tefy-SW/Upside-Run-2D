using UnityEngine;

// Identifica qué recoge o toca el jugador.
public class Objeto : MonoBehaviour
{
    public enum Tipo { Obstaculo, Gema, Cassette }
    public Tipo tipo;
    [HideInInspector] public bool recogido;
}
