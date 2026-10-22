using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic customer review engine ("Yelp for Dishaster").
/// Generates context-aware customer quotes and ratings for the end-of-day summary
/// based on dishes delivered, tip amounts, crises/fires, and food temperature.
/// </summary>
public static class CustomerReviewManager
{
    [Serializable]
    public struct CustomerReviewCard
    {
        public string authorHandle;
        public int stars;
        public string comment;
    }

    private static readonly string[] PositiveAuthors = new[]
    {
        "@BurgerLover99", "@CityFoodie_Dan", "@SpeedyScooterFan", "@GourmetChefTina", "@BoardwalkGourmet", "@NightShiftNurse"
    };

    private static readonly string[] NeutralAuthors = new[]
    {
        "@HungryCommuter", "@DowntownSam", "@LunchBreaker", "@CasualDiner_Lou", "@PatioEater"
    };

    private static readonly string[] CriticalAuthors = new[]
    {
        "@KarenReviews", "@ImpatientFoodie", "@ColdFoodComplainer", "@StrictCritic_Bob", "@HangryCustomer"
    };

    private static readonly string[] CrisisAuthors = new[]
    {
        "@ChaosEnthusiast", "@FireWitness_Zack", "@DramaFoodie", "@WildWednesday"
    };

    public static List<CustomerReviewCard> GenerateDailyReviews(
        int dishesDelivered, 
        float averageRating, 
        int tips, 
        int fines, 
        bool isDebt)
    {
        var list = new List<CustomerReviewCard>();

        // 1. Primary Review (Based on Overall Score)
        if (averageRating >= 4.2f && dishesDelivered > 0)
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = PositiveAuthors[UnityEngine.Random.Range(0, PositiveAuthors.Length)],
                stars = 5,
                comment = GetRandomPositiveQuote()
            });
        }
        else if (averageRating >= 3.0f)
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = NeutralAuthors[UnityEngine.Random.Range(0, NeutralAuthors.Length)],
                stars = 3,
                comment = GetRandomNeutralQuote()
            });
        }
        else
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = CriticalAuthors[UnityEngine.Random.Range(0, CriticalAuthors.Length)],
                stars = 1,
                comment = GetRandomCriticalQuote()
            });
        }

        // 2. Secondary Review (Based on Kitchen Crises or Delivery Speed)
        if (fines > 0)
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = CrisisAuthors[UnityEngine.Random.Range(0, CrisisAuthors.Length)],
                stars = 3,
                comment = GetRandomCrisisQuote()
            });
        }
        else if (tips >= 40)
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = PositiveAuthors[UnityEngine.Random.Range(0, PositiveAuthors.Length)],
                stars = 5,
                comment = "Scooter courier arrived at lightning speed! Food was steaming hot. Left a big tip!"
            });
        }
        else
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = NeutralAuthors[UnityEngine.Random.Range(0, NeutralAuthors.Length)],
                stars = 4,
                comment = "Solid burger and crispy fries. Nice clean diner atmosphere."
            });
        }

        // 3. Tertiary Review (Combo or General Ambience)
        if (dishesDelivered >= 6)
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = "@FoodieExplorer",
                stars = 5,
                comment = "The Deluxe Burger Combo is unbeatable. Fountain soda was cold, fries perfectly salted!"
            });
        }
        else
        {
            list.Add(new CustomerReviewCard
            {
                authorHandle = "@LocalNeighbor",
                stars = averageRating >= 3.5f ? 4 : 2,
                comment = averageRating >= 3.5f 
                    ? "Cozy spot! Glad to see them surviving the daily city rush."
                    : "Took a bit long to get our table order, hope service speeds up tomorrow."
            });
        }

        return list;
    }

    private static string GetRandomPositiveQuote()
    {
        string[] quotes = new[]
        {
            "Best burger in the city! Delivered blazing fast on an electric scooter!",
            "Deluxe Combo was pure perfection, arrived piping hot and fresh!",
            "Fastest dishwashing and order turnaround I've ever seen. 10/10!",
            "Incredible food! The chef team was firing on all cylinders today."
        };
        return quotes[UnityEngine.Random.Range(0, quotes.Length)];
    }

    private static string GetRandomNeutralQuote()
    {
        string[] quotes = new[]
        {
            "Decent lunch rush service. Burger was warm, fries were okay.",
            "Tables were a bit crowded, but the kitchen staff kept up well.",
            "Good value for money. Wish the delivery arrived a few minutes sooner."
        };
        return quotes[UnityEngine.Random.Range(0, quotes.Length)];
    }

    private static string GetRandomCriticalQuote()
    {
        string[] quotes = new[]
        {
            "Waited forever and the food was stone cold by the time it arrived!",
            "Order was delayed and courier looked exhausted. 1 star.",
            "Tables weren't cleaned fast enough! Never ordering again."
        };
        return quotes[UnityEngine.Random.Range(0, quotes.Length)];
    }

    private static string GetRandomCrisisQuote()
    {
        string[] quotes = new[]
        {
            "The kitchen literally caught fire while I was eating my salad! 3 stars for the excitement.",
            "Water pipes burst and a chef ran past with a pipe wrench. Dinner and a show!",
            "Saw the courier crash into a fire hydrant and keep right on driving. Dedicated staff!"
        };
        return quotes[UnityEngine.Random.Range(0, quotes.Length)];
    }
}
