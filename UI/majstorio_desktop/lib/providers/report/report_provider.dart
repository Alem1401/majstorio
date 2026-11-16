import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/report/report_filter.dart';
import 'package:majstorio_desktop/models/report/request_report_row.dart';
import 'package:majstorio_desktop/models/report/tradesperson_report_row.dart';

class ReportProvider extends ChangeNotifier {
  Future<List<RequestReportRow>> getRequestReport(ReportFilter filter) async {
    var rows = await _getRows("Reports/requests", filter);
    return rows.map((row) => RequestReportRow.fromJson(row)).toList();
  }

  Future<List<TradespersonReportRow>> getTradespersonReport(
    ReportFilter filter,
  ) async {
    var rows = await _getRows("Reports/tradespeople", filter);
    return rows.map((row) => TradespersonReportRow.fromJson(row)).toList();
  }

  // Both reports return a plain JSON list of rows.
  Future<List<dynamic>> _getRows(String route, ReportFilter filter) async {
    var response = await http.get(
      Uri.parse(
        "$baseUrl/$route",
      ).replace(queryParameters: filter.toQueryParameters()), headers: authHeaders());
    checkUnauthorized(response);
    if (response.statusCode != 200) {
      throw Exception("The report could not be loaded.");
    }
    return json.decode(response.body) as List;
  }
}
