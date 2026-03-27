using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Deck : MonoBehaviour
{
    [Header("Configuración de Cartas")]
    public Sprite[] faces;
    public int[] values = new int[52];
    private int cardIndex = 0;

    [Header("Referencias de Juego")]
    public GameObject dealer;
    public GameObject player;
    private CardHand dealerHand;
    private CardHand playerHand;

    [Header("UI Interfaz")]
    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Text finalMessage;
    public Text probMessage;
    public Text bankMessage;
    public Text betMessage;

    [Header("Lógica de Apuestas")]
    private int banca = 1000;
    private int apuestaActual = 10;
    private bool gameOver = false;
    private bool dealerFirstCardHidden = true;

    private void Awake()
    {
        dealerHand = dealer.GetComponent<CardHand>();
        playerHand = player.GetComponent<CardHand>();
        InitCardValues();
        UpdateMoneyUI();
    }

    private void Start()
    {
        ShuffleCards();
        StartGame();
    }

    private void InitCardValues()
    {
        for (int i = 0; i < values.Length; i++)
        {
            int rank = (i % 13) + 1;
            // Guardamos el As siempre como 11 inicialmente. 
            // La función CalculateHandValue se encargará de convertirlo a 1 si es necesario.
            if (rank == 1) values[i] = 11;
            else if (rank >= 10) values[i] = 10;
            else values[i] = rank;
        }
    }

    private void ShuffleCards()
    {
        cardIndex = 0;
        for (int i = 0; i < faces.Length; i++)
        {
            int randomIndex = Random.Range(i, faces.Length);

            Sprite tempFace = faces[i];
            faces[i] = faces[randomIndex];
            faces[randomIndex] = tempFace;

            int tempValue = values[i];
            values[i] = values[randomIndex];
            values[randomIndex] = tempValue;
        }
    }

    // Método de seguridad para obtener cartas
    private void VerifyDeck()
    {
        if (cardIndex >= faces.Length)
        {
            Debug.Log("Mazo vacío, barajando de nuevo...");
            ShuffleCards();
        }
    }

    void StartGame()
    {
        // 1. Validaciones previas
        if (banca < 10 && banca < apuestaActual)
        {
            finalMessage.text = "Banca insuficiente.";
            ToggleButtons(false, false, false);
            return;
        }

        // 2. Reset de estado
        gameOver = false;
        dealerFirstCardHidden = true;
        finalMessage.text = "";
        ToggleButtons(true, true, false);

        // 3. Cobrar apuesta
        banca -= apuestaActual;
        UpdateMoneyUI();

        // 4. Repartir inicial
        for (int i = 0; i < 2; i++)
        {
            PushPlayer();
            PushDealer();
        }

        // 5. Verificar Blackjacks Naturales
        CheckInitialBlackjack();
    }

    private void CheckInitialBlackjack()
    {
        int pPoints = CalculateHandValue(GetHandValues(playerHand));
        int dPoints = CalculateHandValue(GetHandValues(dealerHand));

        if (pPoints == 21 || dPoints == 21)
        {
            if (pPoints == 21 && dPoints == 21)
            {
                banca += apuestaActual; // Devolver apuesta
                EndGame("Empate: ambos tienen Blackjack.");
            }
            else if (pPoints == 21)
            {
                // El Blackjack suele pagar 3 a 2, aquí lo dejamos 2 a 1 (apuesta * 2)
                banca += apuestaActual * 2;
                EndGame("¡Blackjack! Ganas.");
            }
            else
            {
                EndGame("Dealer tiene Blackjack. Pierdes.");
            }
        }
    }

    public void Hit()
    {
        if (gameOver) return;

        PushPlayer();

        if (CalculateHandValue(GetHandValues(playerHand)) > 21)
        {
            EndGame("Te pasas de 21. Pierdes.");
        }
    }

    public void Stand()
    {
        if (gameOver) return;

        RevealDealerCard();

        // El Dealer pide hasta tener 17 o más
        while (CalculateHandValue(GetHandValues(dealerHand)) < 17)
        {
            PushDealer();
        }

        int pPoints = CalculateHandValue(GetHandValues(playerHand));
        int dPoints = CalculateHandValue(GetHandValues(dealerHand));

        if (dPoints > 21)
        {
            banca += apuestaActual * 2;
            EndGame("El dealer se pasa. Ganas.");
        }
        else if (dPoints > pPoints)
        {
            EndGame("Gana el dealer.");
        }
        else if (dPoints < pPoints)
        {
            banca += apuestaActual * 2;
            EndGame("¡Ganas!");
        }
        else
        {
            banca += apuestaActual;
            EndGame("Empate.");
        }
    }

    private void PushDealer()
    {
        VerifyDeck();
        dealerHand.Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
    }

    private void PushPlayer()
    {
        VerifyDeck();
        playerHand.Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        CalculateProbabilities();
    }

    public void PlayAgain()
    {
        playerHand.Clear();
        dealerHand.Clear();
        // Opcional: ShuffleCards() aquí si quieres barajar cada mano
        StartGame();
    }

    private void EndGame(string message)
    {
        gameOver = true;
        RevealDealerCard();
        finalMessage.text = message;
        ToggleButtons(false, false, true);
        UpdateMoneyUI();
    }

    private void ToggleButtons(bool hit, bool stick, bool playAgain)
    {
        hitButton.interactable = hit;
        stickButton.interactable = stick;
        playAgainButton.interactable = playAgain;
    }

    private void UpdateMoneyUI()
    {
        if (bankMessage) bankMessage.text = "Banca: " + banca + "€";
        if (betMessage) betMessage.text = "Apuesta: " + apuestaActual + "€";
    }

    private List<int> GetHandValues(CardHand hand)
    {
        List<int> handValues = new List<int>();
        foreach (GameObject card in hand.cards)
        {
            handValues.Add(card.GetComponent<CardModel>().value);
        }
        return handValues;
    }

    private int CalculateHandValue(List<int> handValues)
    {
        int total = 0;
        int aces = 0;

        foreach (int value in handValues)
        {
            if (value == 11) aces++;
            total += value;
        }

        // Si nos pasamos de 21, convertimos Ases de 11 a 1 (restando 10)
        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return total;
    }

    private void RevealDealerCard()
    {
        if (dealerFirstCardHidden)
        {
            dealerHand.InitialToggle();
            dealerFirstCardHidden = false;
        }
    }

    // --- Lógica de Probabilidades Corregida ---
    private void CalculateProbabilities()
    {
        if (gameOver || cardIndex >= values.Length) return;

        int remaining = values.Length - cardIndex;
        int bustCount = 0;
        int currentPoints = CalculateHandValue(GetHandValues(playerHand));

        for (int i = cardIndex; i < values.Length; i++)
        {
            // Simulamos pedir una carta
            List<int> virtualHand = GetHandValues(playerHand);
            virtualHand.Add(values[i]);
            if (CalculateHandValue(virtualHand) > 21) bustCount++;
        }

        float probBust = (float)bustCount / remaining * 100f;
        probMessage.text = $"Probabilidad de pasarse: {probBust:F2}%";
    }

    public void IncreaseBet() { if (gameOver || playerHand.cards.Count == 0) { apuestaActual += 10; UpdateMoneyUI(); } }
    public void DecreaseBet() { if (gameOver || playerHand.cards.Count == 0) { if (apuestaActual > 10) apuestaActual -= 10; UpdateMoneyUI(); } }
}