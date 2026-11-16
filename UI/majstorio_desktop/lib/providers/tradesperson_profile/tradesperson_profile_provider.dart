import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

class TradespersonProfileProvider extends BaseProvider<TradespersonProfile> {
  TradespersonProfileProvider() : super("TradespersonProfiles", TradespersonProfile.fromJson);

  // Approving the profile is what gives its user the Tradesperson role.
  Future<int> approve(TradespersonProfile profile) async {
    var response = await http.post(
      Uri.parse("$baseUrl/TradespersonProfiles/${profile.id}/approve"),
      headers: authHeaders(),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }

  // An inactive profile is hidden from customers and cannot send offers.
  Future<int> setActive(TradespersonProfile profile, bool isActive) async {
    var response = await http.put(
      Uri.parse("$baseUrl/TradespersonProfiles/${profile.id}/active"),
      headers: authHeaders(),
      body: json.encode({"isActive": isActive}),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
