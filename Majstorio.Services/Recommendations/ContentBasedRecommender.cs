using Majstorio.Services.Constants;

namespace Majstorio.Services.Recommendations;

// The user's interest per city and category, built from their searches and service requests.
public sealed class UserPreferences
{
    private readonly Dictionary<int, double> _categories = [];
    private readonly Dictionary<int, double> _cities = [];

    public IReadOnlyDictionary<int, double> Categories => _categories;
    public IReadOnlyDictionary<int, double> Cities => _cities;
    public bool IsEmpty => _categories.Count == 0 && _cities.Count == 0;

    public void Add(int? categoryId, int? cityId, double weight)
    {
        if (categoryId.HasValue)
            _categories[categoryId.Value] = _categories.GetValueOrDefault(categoryId.Value) + weight;

        if (cityId.HasValue)
            _cities[cityId.Value] = _cities.GetValueOrDefault(cityId.Value) + weight;
    }
}

// The features of one tradesperson that take part in scoring.
public sealed record RecommendationCandidate(
    int ProfileId, int CityId, List<int> CategoryIds, double? AverageRating, int CompletedJobs);

public sealed record Recommendation(int ProfileId, double Score, List<string> Reasons);

// Content-based filtering with cosine similarity between the user's preference vector and the
// tradesperson's feature vector, combined with the normalised rating. A user without history
// gets a popularity ranking instead (cold start). Described in recommender-dokumentacija.md.
public static class ContentBasedRecommender
{
    public const double CategoryWeight = 0.5;
    public const double CityWeight = 0.3;
    public const double RatingWeight = 0.2;
    private const double MaximumRating = 5;

    public static List<Recommendation> Rank(UserPreferences preferences, IReadOnlyCollection<RecommendationCandidate> candidates)
    {
        var maximumCompletedJobs = candidates.Count == 0 ? 0 : candidates.Max(candidate => candidate.CompletedJobs);

        return candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Result = preferences.IsEmpty
                    ? ScoreByPopularity(candidate, maximumCompletedJobs)
                    : ScoreByContent(preferences, candidate)
            })
            .OrderByDescending(item => item.Result.Score)
            .ThenByDescending(item => item.Candidate.AverageRating ?? 0)
            .ThenBy(item => item.Candidate.ProfileId)
            .Select(item => item.Result)
            .ToList();
    }

    // cos(u, v) = (u . v) / (|u| * |v|), where v is a binary vector: 1 for every feature the item has.
    public static double CosineSimilarity(IReadOnlyDictionary<int, double> userVector, IReadOnlyCollection<int> itemFeatures)
    {
        if (userVector.Count == 0 || itemFeatures.Count == 0)
            return 0;

        var dotProduct = itemFeatures.Sum(feature => userVector.GetValueOrDefault(feature));
        var userLength = Math.Sqrt(userVector.Values.Sum(weight => weight * weight));
        var itemLength = Math.Sqrt(itemFeatures.Count);

        return userLength == 0 ? 0 : dotProduct / (userLength * itemLength);
    }

    private static Recommendation ScoreByContent(UserPreferences preferences, RecommendationCandidate candidate)
    {
        var category = CategoryWeight * CosineSimilarity(preferences.Categories, candidate.CategoryIds);
        var city = CityWeight * CosineSimilarity(preferences.Cities, [candidate.CityId]);
        var rating = RatingWeight * NormalizedRating(candidate);

        // The strongest contribution is named first; a signal that added nothing is not claimed.
        var reasons = new[]
            {
                (Contribution: category, Reason: RecommendationReasons.Category),
                (Contribution: city, Reason: RecommendationReasons.City),
                (Contribution: rating, Reason: RecommendationReasons.Rating)
            }
            .Where(part => part.Contribution > 0)
            .OrderByDescending(part => part.Contribution)
            .Select(part => part.Reason)
            .ToList();

        return new Recommendation(candidate.ProfileId, category + city + rating, reasons);
    }

    // Popular means many completed jobs and good ratings. The logarithm keeps one very busy
    // tradesperson from pushing everyone else far down.
    private static Recommendation ScoreByPopularity(RecommendationCandidate candidate, int maximumCompletedJobs)
    {
        var jobs = maximumCompletedJobs == 0
            ? 0
            : Math.Log(1 + candidate.CompletedJobs) / Math.Log(1 + maximumCompletedJobs);
        var rating = NormalizedRating(candidate);

        var reasons = new List<string>();
        if (candidate.CompletedJobs > 0)
            reasons.Add(RecommendationReasons.Popularity);
        if (rating > 0)
            reasons.Add(RecommendationReasons.Rating);

        return new Recommendation(candidate.ProfileId, jobs * rating, reasons);
    }

    private static double NormalizedRating(RecommendationCandidate candidate) =>
        (candidate.AverageRating ?? 0) / MaximumRating;
}
