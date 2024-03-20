using System;
using System.Collections.Generic;

namespace TTRPGCreator.Other
{
    public class CardSystem
    {
        public string[] cardValues = { ":a:", "2", "3", "4", "5", "6", "7", "8", "9", "10", ":regional_indicator_k:", ":regional_indicator_q:", ":crown:" };
        public string[] cardSuits = { ":hearts:", ":diamonds:", ":clubs:", ":spades:" };

        private Card[] cards = new Card[52];
        private List<Card> deck;

        public CardSystem()
        {
            deck = new List<Card>();

            for (int i = 0; i < cardSuits.Length; i++)
            {
                for (int j = 0; j < cardValues.Length; j++)
                {
                    cards[i * cardValues.Length + j] = new Card { value = cardValues[j], suit = cardSuits[i] };
                }
            }

            Shuffle();
        }

        public void Shuffle()
        {
            deck.Clear();
            foreach (Card card in cards)
            {
                deck.Add(card);
            }
        }

        public Card GetCard()
        {
            if(deck.Count == 0)
            {
                Shuffle();
            }

            var random = new Random();
            Card card = deck[random.Next(deck.Count)];
            deck.Remove(card);

            return card;
        }
    }

    public struct Card
    {
        public string value;
        public string suit;
    }
}
