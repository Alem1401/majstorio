import 'package:majstorio_mobile/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Tradesperson search and profile. The API returns 404 for a profile the user may not see.
class TradespersonProfileProvider extends BaseProvider<TradespersonProfile> {
  TradespersonProfileProvider() : super("TradespersonProfiles", TradespersonProfile.fromJson);

  Future<TradespersonProfileDetail> getById(int id) async {
    return TradespersonProfileDetail.fromJson(await getJson("TradespersonProfiles/$id"));
  }
}
