import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/service_request/service_request.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

class ServiceRequestProvider extends BaseProvider<ServiceRequest> {
  ServiceRequestProvider() : super("ServiceRequests", ServiceRequest.fromJson);

  // The request with its offers, appointment and status history.
  Future<ServiceRequestDetail> getById(int id) async {
    var response = await http.get(
      Uri.parse("$baseUrl/ServiceRequests/$id"),
      headers: authHeaders(),
    );
    checkUnauthorized(response);
    return ServiceRequestDetail.fromJson(json.decode(response.body));
  }

  // The API cancels the request, its appointment and its open offers, and writes the status history.
  Future<int> cancel(int id) async {
    var response = await http.post(
      Uri.parse("$baseUrl/ServiceRequests/$id/cancel"),
      headers: authHeaders(),
      body: json.encode({"reason": "Cancelled by the administrator."}),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
