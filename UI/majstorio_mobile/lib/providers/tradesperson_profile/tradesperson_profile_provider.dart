import 'package:flutter/foundation.dart';
import 'package:majstorio_mobile/models/base_search_request.dart';
import 'package:majstorio_mobile/models/paged_result.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile_search_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Tradesperson search and profile. The API returns 404 for a profile the user may not see.
class TradespersonProfileProvider extends BaseProvider<TradespersonProfile> {
  TradespersonProfileProvider() : super("TradespersonProfiles", TradespersonProfile.fromJson);

  // Counts the searches by category or city. The API remembers them, so the recommendations change.
  final searches = ValueNotifier<int>(0);

  @override
  Future<PagedResult<TradespersonProfile>> get(BaseSearchRequest request) async {
    var result = await super.get(request);
    if (request is TradespersonProfileSearchRequest &&
        (request.cityId != null || request.serviceCategoryId != null)) {
      searches.value++;
    }
    return result;
  }

  Future<TradespersonProfileDetail> getById(int id) async {
    return TradespersonProfileDetail.fromJson(await getJson("TradespersonProfiles/$id"));
  }
}
