import 'dart:convert';

import 'package:http/http.dart' as http;

// A failed API call. The screen shows "message"; "statusCode" tells it what kind of failure it was
// (for example 409 = the record changed in the meantime, so the screen reloads it).
// "fieldErrors" holds the validation errors per field, so a form can show them under the right input.
class ApiException implements Exception {
  ApiException(this.statusCode, this.message, {this.fieldErrors = const {}});

  final int? statusCode;
  final String message;
  final Map<String, String> fieldErrors;

  @override
  String toString() => message;
}

// Turns a failed answer into a sentence for the user.
// The API sends { "message": "..." } for business errors and { "errors": { "Field": ["..."] } } for validation.
String errorMessage(http.Response response) {
  try {
    var body = json.decode(response.body);
    if (body is Map) {
      if (body["message"] is String && (body["message"] as String).isNotEmpty) {
        return body["message"];
      }
      if (body["errors"] is Map) {
        var errors = (body["errors"] as Map).values
            .expand((value) => value is List ? value : [value])
            .map((value) => value.toString())
            .toList();
        if (errors.isNotEmpty) return errors.join("\n");
      }
    }
  } catch (e) {
    // Empty or non-JSON body (401/403 have none): fall back to the status below.
  }
  return statusMessage(response.statusCode);
}

// The validation errors of a 400 answer, keyed by field name in camelCase ("price", "desiredStartUtc").
Map<String, String> fieldErrors(http.Response response) {
  try {
    var body = json.decode(response.body);
    if (body is Map && body["errors"] is Map) {
      var result = <String, String>{};
      (body["errors"] as Map).forEach((key, value) {
        var name = key.toString();
        name = name.isEmpty ? name : name[0].toLowerCase() + name.substring(1);
        result[name] = value is List ? value.join("\n") : value.toString();
      });
      return result;
    }
  } catch (e) {
    // No field errors in this answer.
  }
  return {};
}

String statusMessage(int statusCode) {
  if (statusCode == 400) return "Uneseni podaci nisu ispravni.";
  if (statusCode == 401) return "Sesija je istekla. Prijavite se ponovo.";
  if (statusCode == 403) return "Nemate dozvolu za ovu radnju.";
  if (statusCode == 404) return "Zapis nije dostupan.";
  if (statusCode == 409) return "Podaci su se u međuvremenu promijenili. Osvježite i pokušajte ponovo.";
  if (statusCode == 413) return "Datoteka je prevelika.";
  return "Došlo je do greške na serveru. Pokušajte ponovo.";
}

// The text to show for any error caught in a screen.
String exceptionText(Object error) {
  if (error is ApiException) return error.message;
  var text = error.toString();
  return text.startsWith("Exception: ") ? text.substring(11) : text;
}
