import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// JSON the way the API sends it, and a fake API for the tests.

Map<String, dynamic> requestJson({int id = 10, int status = 0, int customerId = 5, int? targetProfileId}) => {
  'id': id,
  'customerId': customerId,
  'customerName': 'Mobile Test',
  'cityId': 1,
  'cityName': 'Mostar',
  'serviceCategoryId': 2,
  'serviceCategoryName': 'Vodoinstalaterski radovi',
  'targetTradespersonProfileId': targetProfileId,
  'description': 'Curi slavina',
  'address': null,
  'desiredStartUtc': '2030-01-10T10:00:00',
  'maximumBudget': 100,
  'status': status,
  'createdAtUtc': '2026-11-20T10:00:00',
};

Map<String, dynamic> offerJson({int id = 1, int status = 0, double price = 90, int requestId = 10}) => {
  'id': id,
  'serviceRequestId': requestId,
  'tradespersonProfileId': id + 100,
  'customerName': 'Mobile Test',
  'tradespersonName': 'Majstor $id',
  'serviceCategoryName': 'Vodoinstalaterski radovi',
  'price': price,
  'proposedStartUtc': '2030-01-10T10:00:00',
  'durationMinutes': 60,
  'status': status,
  'decisionReason': null,
};

Map<String, dynamic> appointmentJson({int id = 7, int offerId = 1, int status = 1, DateTime? start}) {
  var startUtc = start ?? DateTime.utc(2030, 1, 10, 10);
  return {
    'id': id,
    'offerId': offerId,
    'customerName': 'Mobile Test',
    'tradespersonName': 'Majstor 1',
    'serviceCategoryName': 'Vodoinstalaterski radovi',
    'startUtc': startUtc.toIso8601String(),
    'endUtc': startUtc.add(const Duration(hours: 1)).toIso8601String(),
    'status': status,
  };
}

Map<String, dynamic> historyJson({int id = 1, int newStatus = 0}) => {
  'id': id,
  'changedByUserId': 5,
  'changedByName': 'Mobile Test',
  'changedAtUtc': '2026-11-20T10:00:00',
  'previousStatus': newStatus == 0 ? null : newStatus - 1,
  'newStatus': newStatus,
  'note': null,
};

Map<String, dynamic> requestDetailJson({
  int status = 0,
  int customerId = 5,
  List<Map<String, dynamic>> offers = const [],
  Map<String, dynamic>? appointment,
  List<Map<String, dynamic>>? history,
}) => {
  ...requestJson(status: status, customerId: customerId),
  'offers': offers,
  'appointment': appointment,
  'statusHistory': history ?? [historyJson()],
};

Map<String, dynamic> page(List<Map<String, dynamic>> items) => {
  'items': items,
  'totalCount': items.length,
  'page': 1,
  'pageSize': 10,
};

// A fake API: each call is answered by "handler"; every request is recorded in "calls".
class FakeApi {
  FakeApi(this.handler) {
    BaseProvider.client = MockClient((request) async {
      calls.add(request);
      return handler(request);
    });
  }

  final http.Response Function(http.Request request) handler;
  final calls = <http.Request>[];
}

http.Response jsonResponse(Object body, [int status = 200]) =>
    http.Response(json.encode(body), status, headers: {'content-type': 'application/json; charset=utf-8'});
