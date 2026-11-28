import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the service list. Mirrors TradespersonServiceSearchRequest on the backend.
class TradespersonServiceSearchRequest extends BaseSearchRequest {
  final int? tradespersonProfileId;
  final String? name;

  TradespersonServiceSearchRequest({super.page, super.pageSize, this.tradespersonProfileId, this.name});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (tradespersonProfileId != null) 'tradespersonProfileId': tradespersonProfileId.toString(),
    if (name != null && name!.trim().isNotEmpty) 'name': name!.trim(),
  };
}
