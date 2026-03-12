using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public GameObject dealer;
    public GameObject player;
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;

    public int[] values = new int[52];
    int cardIndex = 0;

    private int banca = 1000;
    private int apuestaActual = 10;

    private CardHand dealerHand;
    private CardHand playerHand;

    private void Awake()
    {
        dealerHand = dealer.GetComponent<CardHand>();
        playerHand = player.GetComponent<CardHand>();
        InitCardValues();

    }

    private void Start()
    {
        ShuffleCards();
        StartGame();
    }

    private void InitCardValues()
    {
        /// Se asume orden por palo:
        // A,2,3,4,5,6,7,8,9,10,J,Q,K  x4
        for (int i = 0; i < values.Length; i++)
        {
            int rank = i % 13;

            if (rank == 0)           // As
                values[i] = 11;
            else if (rank >= 10)     // J, Q, K
                values[i] = 10;
            else                     // 2..10
                values[i] = rank + 1;
        }
    }

    private void ShuffleCards()
    {
        // Fisher-Yates sincronizando faces y values
        for (int i = faces.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;

            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }
    }

    void StartGame()
    {
        // Reparto inicial:
        // jugador, dealer, jugador, dealer
        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        // Comprobar blackjack inicial
        if (playerHand.points == 21 || dealerHand.points == 21)
        {
            // Mostrar la carta oculta del dealer
            dealerHand.InitialToggle();

            if (playerHand.points == 21 && dealerHand.points == 21)
            {
                EndRound("Empate. Ambos tienen Blackjack.", 0);
            }
            else if (playerHand.points == 21)
            {
                EndRound("Blackjack. Gana el jugador.", apuestaActual);
            }
            else
            {
                EndRound("Blackjack del dealer. Pierdes.", -apuestaActual);
            }
        }
        else
        {
            CalculateProbabilities();
        }
    }

    private void CalculateProbabilities()
    {
        int remainingCards = values.Length - cardIndex;

        if (remainingCards <= 0)
        {
            probMessage.text = "No quedan cartas en el mazo.";
            return;
        }

        int safeCards = 0;       // cartas con las que no se pasa
        int blackjackCards = 0;  // cartas con las que llega a 21
        int bustCards = 0;       // cartas con las que se pasa

        for (int i = cardIndex; i < values.Length; i++)
        {
            int simulatedPoints = SimulatePlayerPointsAfterDraw(values[i]);

            if (simulatedPoints > 21)
            {
                bustCards++;
            }
            else
            {
                safeCards++;

                if (simulatedPoints == 21)
                    blackjackCards++;
            }
        }

        float probSafe = (float)safeCards / remainingCards * 100f;
        float probBlackjack = (float)blackjackCards / remainingCards * 100f;
        float probBust = (float)bustCards / remainingCards * 100f;

        probMessage.text =
            "No pasarse: " + probSafe.ToString("F1") + "%\n" +
            "Llegar a 21: " + probBlackjack.ToString("F1") + "%\n" +
            "Pasarse: " + probBust.ToString("F1") + "%";
    }

    private int SimulatePlayerPointsAfterDraw(int newCardValue)
    {
        // Simulamos la mano del jugador con la nueva carta
        int val = 0;
        int aces = 0;

        // Cartas actuales del jugador
        foreach (GameObject c in playerHand.cards)
        {
            int cardValue = c.GetComponent<CardModel>().value;

            if (cardValue == 11)
                aces++;
            else
                val += cardValue;
        }

        // Nueva carta hipotética
        if (newCardValue == 11)
            aces++;
        else
            val += newCardValue;

        // Igual que en CardHand.Push()
        for (int i = 0; i < aces; i++)
        {
            if (val + 11 <= 21)
                val += 11;
            else
                val += 1;
        }

        return val;
    }

    void PushDealer()
    {
        if (cardIndex >= faces.Length)
            return;

        dealerHand.Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    void PushPlayer()
    {
        if (cardIndex >= faces.Length)
            return;

        playerHand.Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        CalculateProbabilities();
    }

    public void Hit()
    {
        if (!hitButton.interactable)
            return;

        PushPlayer();

        if (playerHand.points > 21)
        {
            dealerHand.InitialToggle();
            EndRound("Te has pasado de 21. Pierdes.", -apuestaActual);
        }
        else if (playerHand.points == 21)
        {
            Stand();
        }
        else
        {
            CalculateProbabilities();
        }

    }

    public void Stand()
    {
        if (!stickButton.interactable)
            return;

        hitButton.interactable = false;
        stickButton.interactable = false;

        // Mostrar la carta oculta del dealer
        dealerHand.InitialToggle();

        // El dealer roba con 16 o menos
        while (dealerHand.points <= 16 && cardIndex < faces.Length)
        {
            PushDealer();
        }

        // Comparar resultados
        if (dealerHand.points > 21)
        {
            EndRound("El dealer se pasa. Ganas.", apuestaActual);
        }
        else if (dealerHand.points > playerHand.points)
        {
            EndRound("Gana el dealer.", -apuestaActual);
        }
        else if (dealerHand.points < playerHand.points)
        {
            EndRound("Gana el jugador.", apuestaActual);
        }
        else
        {
            EndRound("Empate.", 0);
        }

    }

    private void EndRound(string message, int cambioBanca)
    {
        hitButton.interactable = false;
        stickButton.interactable = false;

        banca += cambioBanca;

        finalMessage.text = message + "\nBanca: " + banca + "€";
        probMessage.text = "";
    }

    public void PlayAgain()
    {
        hitButton.interactable = true;
        stickButton.interactable = true;
        finalMessage.text = "";
        probMessage.text = "";

        playerHand.Clear();
        dealerHand.Clear();

        cardIndex = 0;
        ShuffleCards();
        StartGame();
    }

}
