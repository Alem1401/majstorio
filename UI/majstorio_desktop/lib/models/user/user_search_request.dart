import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the user list. Mirrors UserSearchRequest on the backend.
class UserSearchRequest extends BaseSearchRequest {
  // Matches the user name, first name or last name.
  final String? name;

  UserSearchRequest({super.page, super.pageSize, this.name});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.trim().isNotEmpty) 'name': name!.trim(),
  };
}
