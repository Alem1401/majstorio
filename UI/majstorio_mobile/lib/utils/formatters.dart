// Reads a time from the API. The API sends UTC times, sometimes without the "Z" mark,
// so a value without a zone is still treated as UTC.
DateTime parseUtc(String value) {
  var hasZone = value.endsWith("Z") || RegExp(r"[+-]\d\d:?\d\d$").hasMatch(value);
  return DateTime.parse(hasZone ? value : "${value}Z").toUtc();
}

DateTime? parseUtcOrNull(String? value) => value == null ? null : parseUtc(value);

String _two(int number) => number.toString().padLeft(2, "0");

// Shows a UTC time from the API in the phone's local time: 05.11.2026 14:30
String formatDateTime(DateTime value) {
  var local = value.toLocal();
  return "${_two(local.day)}.${_two(local.month)}.${local.year} "
      "${_two(local.hour)}:${_two(local.minute)}";
}

// Only the day: 05.11.2026
String formatDate(DateTime value) {
  var local = value.toLocal();
  return "${_two(local.day)}.${_two(local.month)}.${local.year}";
}

// Amounts are in EUR: 120,50 €
String formatMoney(double value) {
  var text = value.toStringAsFixed(2).replaceAll(".", ",");
  return "$text €";
}

// 90 -> "1 h 30 min"
String formatDuration(int minutes) {
  var hours = minutes ~/ 60;
  var rest = minutes % 60;
  if (hours == 0) return "$rest min";
  if (rest == 0) return "$hours h";
  return "$hours h $rest min";
}

// The date sent to the API: UTC in ISO format, ending in "Z".
String toApiDate(DateTime value) => value.toUtc().toIso8601String();
