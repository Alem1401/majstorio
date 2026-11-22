import 'package:majstorio_mobile/models/review/review.dart';
import 'package:majstorio_mobile/models/review/review_search_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// The signed-in customer's ratings (GET/POST /Reviews/mine).
// The API accepts one rating per completed job, only on the customer's own requests.
class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super("Reviews/mine", Review.fromJson);

  // The customer's rating of this appointment, or null when it is not rated yet.
  Future<Review?> getForAppointment(int appointmentId) async {
    var page = await get(ReviewSearchRequest(appointmentId: appointmentId, pageSize: 1));
    return page.items.isEmpty ? null : page.items.first;
  }

  Future<void> insert(int appointmentId, int rating, String? comment) async {
    await command("POST", "Reviews/mine", {
      "appointmentId": appointmentId,
      "rating": rating,
      if (comment != null && comment.isNotEmpty) "comment": comment,
    });
  }
}
