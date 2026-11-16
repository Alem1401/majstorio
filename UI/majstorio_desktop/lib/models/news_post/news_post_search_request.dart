import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the news list. Mirrors NewsPostSearchRequest on the backend.
class NewsPostSearchRequest extends BaseSearchRequest {
  final String? title;

  NewsPostSearchRequest({super.page, super.pageSize, this.title});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (title != null && title!.trim().isNotEmpty) 'title': title!.trim(),
  };
}
