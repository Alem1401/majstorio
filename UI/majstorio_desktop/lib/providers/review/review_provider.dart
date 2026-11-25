import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/review/review.dart';
import 'package:majstorio_desktop/models/review/review_upsert_request.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class ReviewProvider extends BaseProvider<Review> {
  ReviewProvider() : super("Reviews", Review.fromJson);

  Future<int> delete(Review review) async {
    var response = await http.delete(Uri.parse("$baseUrl/Reviews/${review.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> update(int id, ReviewUpsertRequest request) async {
    var response = await http.put(
      Uri.parse("$baseUrl/Reviews/$id"),
      headers: authHeaders(),
      body: json.encode(request.toJson()),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
