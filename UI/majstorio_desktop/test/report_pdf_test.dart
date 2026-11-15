import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_desktop/models/report/report_filter.dart';
import 'package:majstorio_desktop/utils/report_pdf.dart';

void main() {
  test('buildReportPdf creates a PDF file with the rows', () async {
    final bytes = await buildReportPdf(
      title: "Tradesperson performance report",
      filters: "from the beginning, to today, city: all",
      headers: ["Tradesperson", "City", "Offers"],
      rows: [
        ["Emir Kovačević", "Mostar", "3"],
        ["Dino Begić", "Sarajevo", "1"],
      ],
    );

    // Every PDF file starts with these characters.
    expect(String.fromCharCodes(bytes.take(5)), "%PDF-");

    // Lets the PDF be opened by hand when PDF_TEST_OUTPUT is set.
    final output = Platform.environment['PDF_TEST_OUTPUT'];
    if (output != null) {
      File(output).writeAsBytesSync(bytes);
    }
  });

  test('buildReportPdf handles a report without rows', () async {
    final bytes = await buildReportPdf(
      title: "Service requests report",
      filters: "city: all",
      headers: ["City", "Requests"],
      rows: [],
    );

    expect(String.fromCharCodes(bytes.take(5)), "%PDF-");
  });

  test('ReportFilter leaves out empty filters and includes the whole last day', () {
    expect(ReportFilter().toQueryParameters(), isEmpty);

    final parameters = ReportFilter(
      fromDate: DateTime(2026, 10, 1),
      toDate: DateTime(2026, 10, 31),
      cityId: 2,
      status: 3,
    ).toQueryParameters();

    expect(parameters['cityId'], "2");
    expect(parameters['status'], "3");
    expect(parameters.containsKey('serviceCategoryId'), isFalse);
    expect(
      DateTime.parse(parameters['fromUtc']!).toLocal(),
      DateTime(2026, 10, 1),
    );
    expect(
      DateTime.parse(parameters['toUtc']!).toLocal(),
      DateTime(2026, 11, 1),
    );
  });
}
