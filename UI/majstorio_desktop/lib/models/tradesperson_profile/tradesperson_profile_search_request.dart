import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the tradesperson list. Mirrors TradespersonProfileSearchRequest on the backend.
class TradespersonProfileSearchRequest extends BaseSearchRequest {
  // Matches the first or last name.
  final String? name;
  // False shows only the profiles that wait for approval.
  final bool? isApproved;

  TradespersonProfileSearchRequest({super.page, super.pageSize, this.name, this.isApproved});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.trim().isNotEmpty) 'name': name!.trim(),
    if (isApproved != null) 'isApproved': isApproved.toString(),
  };
}
