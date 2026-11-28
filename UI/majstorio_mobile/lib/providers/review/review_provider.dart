import 'dart:convert';

import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/models/reviews/review.dart';
import 'package:majstorio_mobile/models/reviews/review_search_request.dart';
import 'package:majstorio_mobile/models/reviews/review_upsert_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

class ReviewProvider extends BaseProvider<ReviewResponse> {
  ReviewProvider() : super("Reviews/mine", ReviewResponse.fromJson);

  Future<int> post(ReviewUpsertRequest request) async {
    var response = await send(
      BaseProvider.client.post(url("Reviews/mine"), body: json.encode(request.toJson()), headers: authHeaders()),
      isCommand: true,
    );
    checkUnauthorized(response);
    return response.statusCode;
  }

  // The rating of this appointment, or null when it is not rated yet.
  Future<ReviewResponse?> getForAppointment(int appointmentId) async {
    var page = await get(ReviewSearchRequest(appointmentId: appointmentId, pageSize: 1));
    return page.items.isEmpty ? null : page.items.first;
  }
}
