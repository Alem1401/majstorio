import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/dashboard/dashboard.dart';

class DashboardProvider extends ChangeNotifier {
  Future<Dashboard> get() async {
    var response = await http.get(Uri.parse("$baseUrl/Dashboard"));
    if (response.statusCode != 200) {
      throw Exception("The dashboard could not be loaded.");
    }
    return Dashboard.fromJson(json.decode(response.body));
  }
}
