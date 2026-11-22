import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for GET /Reviews/mine. Mirrors ReviewSearchRequest on the backend.
class ReviewSearchRequest extends BaseSearchRequest {
  final int? appointmentId;

  ReviewSearchRequest({super.page, super.pageSize, this.appointmentId});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (appointmentId != null) 'appointmentId': appointmentId.toString(),
  };
}
