import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for GET /Reviews/mine.
class ReviewSearchRequest extends BaseSearchRequest {
  ReviewSearchRequest({super.page, super.pageSize, this.appointmentId});

  final int? appointmentId;

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (appointmentId != null) "appointmentId": appointmentId.toString(),
  };
}
