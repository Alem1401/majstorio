import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the review list. Mirrors ReviewSearchRequest on the backend.
class ReviewSearchRequest extends BaseSearchRequest {
  // 1 to 5; null shows every rating.
  final int? rating;

  ReviewSearchRequest({super.page, super.pageSize, this.rating});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (rating != null) 'rating': rating.toString(),
  };
}
