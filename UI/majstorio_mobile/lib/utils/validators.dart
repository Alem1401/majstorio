// Checks used by the forms. Each returns the message to show under the field, or null when the value is fine.

String? requiredText(String? value, String message) {
  if (value == null || value.trim().isEmpty) return message;
  return null;
}

// A positive EUR amount with at most two decimals. "max" is an optional upper limit (the budget for an offer).
String? money(String? value, {double? max}) {
  var text = (value ?? "").trim().replaceAll(",", ".");
  if (text.isEmpty) return "Iznos je obavezan";
  if (!RegExp(r"^\d+(\.\d{1,2})?$").hasMatch(text)) {
    return "Unesite iznos sa najviše dvije decimale";
  }
  var amount = double.parse(text);
  if (amount <= 0) return "Iznos mora biti veći od 0";
  if (max != null && amount > max) return "Iznos ne smije biti veći od budžeta (${max.toStringAsFixed(2)} €)";
  return null;
}

double parseMoney(String value) => double.parse(value.trim().replaceAll(",", "."));

String? email(String? value) {
  var text = (value ?? "").trim();
  if (text.isEmpty) return "E-pošta je obavezna";
  if (!RegExp(r"^[^@\s]+@[^@\s]+\.[^@\s]+$").hasMatch(text)) return "E-pošta nije ispravna";
  return null;
}

// Optional phone number, the same rule as the API: an optional leading "+", then only digits, spaces, "/" and "-",
// with 6 to 20 digits (separators are not counted) and at most 32 characters in total.
String? phone(String? value) {
  var text = (value ?? "").trim();
  if (text.isEmpty) return null;
  var digits = RegExp(r"[0-9]").allMatches(text).length;
  if (text.length > 32 || !RegExp(r"^\+?[0-9 /-]+$").hasMatch(text) || digits < 6 || digits > 20) {
    return "Unesite 6 do 20 cifara (dozvoljeni su razmak, / i -), npr. +387 61 123 456";
  }
  return null;
}

String? password(String? value) {
  if (value == null || value.isEmpty) return "Lozinka je obavezna";
  if (value.length < 4) return "Lozinka mora imati najmanje 4 znaka";
  return null;
}
