import 'dart:io';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/category/category.dart';
import 'package:majstorio_desktop/models/category/category_search_request.dart';
import 'package:majstorio_desktop/models/city/city.dart';
import 'package:majstorio_desktop/models/city/city_search_request.dart';
import 'package:majstorio_desktop/models/report/report_filter.dart';
import 'package:majstorio_desktop/models/report/request_report_row.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/providers/report/report_provider.dart';
import 'package:majstorio_desktop/utils/report_pdf.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The two reports the administrator can choose between.
enum ReportType { requests, tradespeople }

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  late ReportProvider _reportProvider;

  // The options of the city and category dropdowns, loaded from the API.
  var cities = <City>[];
  var categories = <Category>[];

  // The chosen report and filters.
  ReportType reportType = ReportType.requests;
  DateTime? fromDate;
  DateTime? toDate;
  int? cityId;
  int? categoryId;
  int? status;

  // The report that is currently shown; the same headers and rows go into the PDF.
  // Null until "Show report" is clicked.
  List<String>? headers;
  var rows = <List<String>>[];
  var shownTitle = "";
  var shownFilters = "";

  @override
  void initState() {
    _reportProvider = context.read<ReportProvider>();
    loadFilterOptions();
    super.initState();
  }

  void loadFilterOptions() async {
    try {
      var cityPage = await context.read<CityProvider>().get(
        CitySearchRequest(pageSize: 100),
      );
      if (!mounted) return;
      var categoryPage = await context.read<CategoryProvider>().get(
        CategorySearchRequest(pageSize: 100),
      );
      if (!mounted) return;
      setState(() {
        cities = cityPage.items;
        categories = categoryPage.items;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(
        context,
        "Cities and categories could not be loaded. Check that the API is running.",
      );
    }
  }

  String get _title => reportType == ReportType.requests
      ? "Service requests report"
      : "Tradesperson performance report";

  // A sentence that says which filters were used; it is printed on the PDF.
  String get _filterText {
    var parts = <String>[
      "from ${fromDate == null ? 'the beginning' : _date(fromDate!)}",
      "to ${toDate == null ? 'today' : _date(toDate!)}",
      "city: ${_nameOf(cities.where((c) => c.id == cityId).map((c) => c.name))}",
    ];
    if (reportType == ReportType.requests) {
      parts.add(
        "category: ${_nameOf(categories.where((c) => c.id == categoryId).map((c) => c.name))}",
      );
      parts.add("status: ${RequestReportRow.statusNames[status] ?? 'all'}");
    }
    return parts.join(", ");
  }

  String _nameOf(Iterable<String> names) => names.isEmpty ? "all" : names.first;

  String _date(DateTime date) {
    String two(int number) => number.toString().padLeft(2, '0');
    return "${two(date.day)}.${two(date.month)}.${date.year}";
  }

  void showReport() async {
    if (fromDate != null && toDate != null && toDate!.isBefore(fromDate!)) {
      showMessage(context, "The end date must be on or after the start date.");
      return;
    }

    var filter = ReportFilter(
      fromDate: fromDate,
      toDate: toDate,
      cityId: cityId,
      // The tradesperson report has no category and status filter.
      serviceCategoryId: reportType == ReportType.requests ? categoryId : null,
      status: reportType == ReportType.requests ? status : null,
    );

    try {
      List<String> newHeaders;
      List<List<String>> newRows;
      if (reportType == ReportType.requests) {
        var report = await _reportProvider.getRequestReport(filter);
        newHeaders = ["City", "Category", "Status", "Requests"];
        newRows = report
            .map(
              (row) => [
                row.cityName,
                row.serviceCategoryName,
                row.statusName,
                row.requestCount.toString(),
              ],
            )
            .toList();
      } else {
        var report = await _reportProvider.getTradespersonReport(filter);
        newHeaders = [
          "Tradesperson",
          "City",
          "Offers",
          "Completed jobs",
          "Average rating",
        ];
        newRows = report
            .map(
              (row) => [
                row.tradespersonName,
                row.cityName,
                row.offerCount.toString(),
                row.completedJobCount.toString(),
                row.averageRating?.toStringAsFixed(2) ?? "No reviews",
              ],
            )
            .toList();
      }
      if (!mounted) return;
      setState(() {
        headers = newHeaders;
        rows = newRows;
        shownTitle = _title;
        shownFilters = _filterText;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(
        context,
        "The report could not be loaded. Check that the API is running.",
      );
    }
  }

  Future<void> downloadPdf() async {
    try {
      var bytes = await buildReportPdf(
        title: shownTitle,
        filters: shownFilters,
        headers: headers!,
        rows: rows,
      );
      var userProfile = Platform.environment['USERPROFILE'];
      var folder = userProfile == null
          ? null
          : Directory('$userProfile${Platform.pathSeparator}Downloads');
      var hasDownloadsFolder = folder != null && await folder.exists();
      if (!mounted) return;
      if (folder == null || !hasDownloadsFolder) {
        showMessage(context, "The Downloads folder could not be found.");
        return;
      }
      var now = DateTime.now();
      var name = reportType == ReportType.requests
          ? "requests"
          : "tradespeople";
      var file = File(
        "${folder.path}${Platform.pathSeparator}majstorio-$name-report-${now.year}-${now.month}-${now.day}-${now.hour}${now.minute}${now.second}.pdf",
      );
      await file.writeAsBytes(bytes);
      if (!mounted) return;
      showMessage(context, "The report was saved to ${file.path}");
    } catch (e) {
      // Saving can fail, for example when the folder is not writable; show why instead of crashing.
      if (!mounted) return;
      showMessage(context, "The PDF could not be saved: $e");
    }
  }

  // A filter was changed, so the table no longer matches the filters and is hidden.
  void _filterChanged(VoidCallback change) {
    setState(() {
      change();
      headers = null;
      rows = [];
    });
  }

  @override
  Widget build(BuildContext context) {
    var hasReport = headers != null;

    return MasterScreen(
      title: "Reports",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Wrap(
                  spacing: 16,
                  runSpacing: 16,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    SizedBox(
                      width: 300,
                      child: DropdownButtonFormField<ReportType>(
                        initialValue: reportType,
                        decoration: const InputDecoration(
                          labelText: "Report",
                          prefixIcon: Icon(Icons.picture_as_pdf_outlined),
                        ),
                        items: const [
                          DropdownMenuItem(
                            value: ReportType.requests,
                            child: Text("Service requests"),
                          ),
                          DropdownMenuItem(
                            value: ReportType.tradespeople,
                            child: Text("Tradesperson performance"),
                          ),
                        ],
                        onChanged: (value) {
                          if (value == null) return;
                          _filterChanged(() => reportType = value);
                        },
                      ),
                    ),
                    _DateButton(
                      label: "From date",
                      value: fromDate,
                      onChanged: (date) => _filterChanged(() => fromDate = date),
                    ),
                    _DateButton(
                      label: "To date",
                      value: toDate,
                      onChanged: (date) => _filterChanged(() => toDate = date),
                    ),
                    SizedBox(
                      width: 220,
                      child: DropdownButtonFormField<int?>(
                        initialValue: cityId,
                        decoration: const InputDecoration(labelText: "City"),
                        items: [
                          const DropdownMenuItem(
                            value: null,
                            child: Text("All cities"),
                          ),
                          for (final city in cities)
                            DropdownMenuItem(
                              value: city.id,
                              child: Text(city.name),
                            ),
                        ],
                        onChanged: (value) =>
                            _filterChanged(() => cityId = value),
                      ),
                    ),
                    // Only the service requests report is filtered by category and status.
                    if (reportType == ReportType.requests)
                      SizedBox(
                        width: 240,
                        child: DropdownButtonFormField<int?>(
                          initialValue: categoryId,
                          decoration: const InputDecoration(
                            labelText: "Category",
                          ),
                          items: [
                            const DropdownMenuItem(
                              value: null,
                              child: Text("All categories"),
                            ),
                            for (final category in categories)
                              DropdownMenuItem(
                                value: category.id,
                                child: Text(category.name),
                              ),
                          ],
                          onChanged: (value) =>
                              _filterChanged(() => categoryId = value),
                        ),
                      ),
                    if (reportType == ReportType.requests)
                      SizedBox(
                        width: 240,
                        child: DropdownButtonFormField<int?>(
                          initialValue: status,
                          decoration: const InputDecoration(
                            labelText: "Status",
                          ),
                          items: [
                            const DropdownMenuItem(
                              value: null,
                              child: Text("All statuses"),
                            ),
                            for (final entry
                                in RequestReportRow.statusNames.entries)
                              DropdownMenuItem(
                                value: entry.key,
                                child: Text(entry.value),
                              ),
                          ],
                          onChanged: (value) =>
                              _filterChanged(() => status = value),
                        ),
                      ),
                    ElevatedButton.icon(
                      onPressed: showReport,
                      icon: const Icon(Icons.search),
                      label: const Text("Show report"),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),
            Tooltip(
              message: hasReport
                  ? "Save the shown report as a PDF file"
                  : "Click \"Show report\" first",
              child: ElevatedButton.icon(
                onPressed: hasReport ? downloadPdf : null,
                icon: const Icon(Icons.download_outlined),
                label: const Text("Download PDF"),
              ),
            ),
            const SizedBox(height: 16),
            if (hasReport)
              Card(
                child: rows.isEmpty
                    ? const Padding(
                        padding: EdgeInsets.all(24),
                        child: Text(
                          "There is no data for the selected filters.",
                        ),
                      )
                    : LayoutBuilder(
                        builder: (context, constraints) {
                          return SingleChildScrollView(
                            scrollDirection: Axis.horizontal,
                            child: ConstrainedBox(
                              constraints: BoxConstraints(
                                minWidth: constraints.maxWidth,
                              ),
                              child: DataTable(
                                columns: [
                                  for (final header in headers!)
                                    DataColumn(label: Text(header)),
                                ],
                                rows: [
                                  for (final row in rows)
                                    DataRow(
                                      cells: [
                                        for (final cell in row)
                                          DataCell(Text(cell)),
                                      ],
                                    ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
              ),
          ],
        ),
      ),
    );
  }
}

// A button that opens the date picker and shows the picked date; the X clears it.
class _DateButton extends StatelessWidget {
  const _DateButton({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final String label;
  final DateTime? value;
  final ValueChanged<DateTime?> onChanged;

  @override
  Widget build(BuildContext context) {
    String two(int number) => number.toString().padLeft(2, '0');
    var text = value == null
        ? label
        : "$label: ${two(value!.day)}.${two(value!.month)}.${value!.year}";

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        OutlinedButton.icon(
          onPressed: () async {
            var picked = await showDatePicker(
              context: context,
              initialDate: value ?? DateTime.now(),
              firstDate: DateTime(2020),
              lastDate: DateTime(2100),
            );
            if (picked != null) {
              onChanged(picked);
            }
          },
          icon: const Icon(Icons.calendar_today_outlined, size: 18),
          label: Text(text),
        ),
        if (value != null)
          IconButton(
            icon: const Icon(Icons.close, size: 18),
            tooltip: "Clear $label",
            onPressed: () => onChanged(null),
          ),
      ],
    );
  }
}
