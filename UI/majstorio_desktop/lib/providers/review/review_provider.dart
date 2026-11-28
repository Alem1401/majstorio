import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/review/review.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;

class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super("Reviews", Review.fromJson);

  Future<int> delete(Review review) async {
    var response = await http.delete(Uri.parse("$baseUrl/Reviews/${review.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }
}
