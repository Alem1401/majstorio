import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service_upsert_request.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

class TradespersonServiceProvider extends BaseProvider<TradespersonService> {
  TradespersonServiceProvider() : super("TradespersonServices", TradespersonService.fromJson);

  Future<int> post(TradespersonServiceUpsertRequest request) async {
    var response = await http.post(
      Uri.parse("$baseUrl/TradespersonServices"),
      headers: authHeaders(),
      body: json.encode(request.toJson()),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> update(int id, TradespersonServiceUpsertRequest request) async {
    var response = await http.put(
      Uri.parse("$baseUrl/TradespersonServices/$id"),
      headers: authHeaders(),
      body: json.encode(request.toJson()),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> delete(TradespersonService service) async {
    var response = await http.delete(
      Uri.parse("$baseUrl/TradespersonServices/${service.id}"),
      headers: authHeaders(),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
