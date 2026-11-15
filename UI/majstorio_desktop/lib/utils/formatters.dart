// Shows a UTC time from the API in the computer's local time: 05.11.2026 14:30
String formatDateTime(DateTime value) {
  // The API sends UTC times without the "Z" mark, so Dart reads them as local; mark them as UTC first.
  final utc = value.isUtc
      ? value
      : DateTime.utc(
          value.year,
          value.month,
          value.day,
          value.hour,
          value.minute,
          value.second,
        );
  final local = utc.toLocal();
  String two(int number) => number.toString().padLeft(2, '0');
  return '${two(local.day)}.${two(local.month)}.${local.year} '
      '${two(local.hour)}:${two(local.minute)}';
}
