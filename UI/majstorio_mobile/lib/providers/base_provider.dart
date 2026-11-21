import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/models/base_search_request.dart';
import 'package:majstorio_mobile/models/paged_result.dart';
import 'package:majstorio_mobile/utils/error_message.dart';

// The shared part of every provider: the list call, and the helpers the other calls use.
// Every call checks the status before reading the body; a failure becomes an ApiException
// that the screen catches and shows (see exceptionText in utils/error_message.dart).
class BaseProvider<T> extends ChangeNotifier {
  BaseProvider(this.route, this.fromJson);

  String route;
  final T Function(Map<String, dynamic>) fromJson;

  // Tests replace this with a fake client; the app uses the real one.
  static http.Client client = http.Client();

  Uri url(String path, [Map<String, String>? query]) {
    var uri = Uri.parse("$baseUrl/$path");
    return query == null ? uri : uri.replace(queryParameters: query);
  }

  // One page of the list. The request carries the page, the page size and the filters that are set.
  Future<PagedResult<T>> get(BaseSearchRequest request) async {
    var body = await getJson(route, request.toQueryParameters());
    var listJson = body['items'] as List;
    var list = <T>[];
    for (var i = 0; i < listJson.length; i++) {
      list.add(fromJson(listJson[i]));
    }
    int itemCount = body["totalCount"];
    return PagedResult<T>(items: list, totalCount: itemCount);
  }

  // A GET that returns JSON, for example the detail of one record.
  Future<dynamic> getJson(String path, [Map<String, String>? query]) async {
    var response = await send(client.get(url(path, query), headers: authHeaders()));
    checkResponse(response);
    return json.decode(response.body);
  }

  // A POST, PUT or DELETE that changes data. The API answers 200 with an empty body on success.
  Future<void> command(String method, String path, [Object? body]) async {
    var request = http.Request(method, url(path))..headers.addAll(authHeaders());
    // A POST without data still sends "{}", so the API always receives valid JSON.
    if (body != null || method == "POST") request.body = json.encode(body ?? {});
    var response = await send(
      client.send(request).then(http.Response.fromStream),
      isCommand: true,
    );
    checkResponse(response);
    // Screens that show this data listen to the provider and load it again,
    // so lists, details and the home screen never show the old state after a change.
    notifyListeners();
  }

  // Waits for the answer, and turns "no connection" and "no answer in time" into a message.
  // For a change (isCommand) the app cannot know whether the server applied it, so it says so:
  // the user should look at the data again before repeating the action.
  Future<http.Response> send(Future<http.Response> call, {bool isCommand = false}) async {
    try {
      return await call.timeout(requestTimeout);
    } on TimeoutException {
      throw ApiException(
        null,
        isCommand
            ? "Server nije odgovorio na vrijeme. Osvježite podatke prije ponovnog pokušaja."
            : "Server nije odgovorio na vrijeme. Pokušajte ponovo.",
      );
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException(
        null,
        isCommand
            ? "Veza sa serverom je prekinuta. Osvježite podatke prije ponovnog pokušaja."
            : "Server nije dostupan. Provjerite internet vezu.",
      );
    }
  }
}
