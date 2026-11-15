import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/review/review.dart';
import 'package:majstorio_desktop/providers/review/review_provider.dart';
import 'package:majstorio_desktop/screens/review/review_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

class ReviewsScreen extends StatefulWidget {
  const ReviewsScreen({super.key});

  @override
  State<ReviewsScreen> createState() => _ReviewsScreenState();
}

class _ReviewsScreenState extends State<ReviewsScreen> {
  late ReviewProvider _provider;
  var reviews = <Review>[];

  void loadReviews() async {
    var response = await _provider.get(BaseSearchRequest());
    setState(() {
      reviews = response.items;
    });
  }

  @override
  void initState() {
    _provider = context.read<ReviewProvider>();
    loadReviews();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Reviews",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Card(
          child: LayoutBuilder(
            builder: (context, constraints) {
              return SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: ConstrainedBox(
                  constraints: BoxConstraints(minWidth: constraints.maxWidth),
                  child: DataTable(
                    columns: const [
                      DataColumn(label: Text("Customer")),
                      DataColumn(label: Text("Tradesperson")),
                      DataColumn(label: Text("Rating")),
                      DataColumn(label: Text("Comment")),
                      DataColumn(label: Text("Posted at")),
                      DataColumn(label: Text("Update")),
                      DataColumn(label: Text("Delete")),
                    ],
                    rows: reviews.map((review) {
                      return DataRow(
                        cells: [
                          DataCell(Text(review.customerName)),
                          DataCell(Text(review.tradespersonName)),
                          DataCell(
                            Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(
                                  Icons.star,
                                  color: AppTheme.gold,
                                  size: 18,
                                ),
                                const SizedBox(width: 4),
                                Text("${review.rating} / 5"),
                              ],
                            ),
                          ),
                          DataCell(
                            ConstrainedBox(
                              constraints: const BoxConstraints(maxWidth: 320),
                              child: Text(
                                review.comment ?? "",
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ),
                          DataCell(Text(formatDateTime(review.createdAtUtc))),
                          DataCell(
                            IconButton(
                              icon: const Icon(Icons.edit_outlined),
                              color: AppTheme.ink,
                              tooltip: 'Update',
                              onPressed: () async {
                                var success = await Navigator.push<bool>(
                                  context,
                                  MaterialPageRoute(
                                    builder: (ctx) =>
                                        ReviewDetailsScreen(toUpdate: review),
                                  ),
                                );
                                if (success == true) {
                                  loadReviews();
                                }
                              },
                            ),
                          ),
                          DataCell(
                            IconButton(
                              icon: const Icon(Icons.delete_outline),
                              color: AppTheme.danger,
                              tooltip: 'Delete',
                              onPressed: () async {
                                var confirmed = await showConfirmDialog(
                                  context,
                                  "Are you sure you want to delete the review by ${review.customerName}, this action can't be undone !",
                                );
                                if (!confirmed) return;
                                var statusCode = await _provider.delete(review);
                                if (!context.mounted) return;
                                if (statusCode == 200) {
                                  showMessage(context, "The review was deleted.");
                                  loadReviews();
                                } else {
                                  showMessage(
                                    context,
                                    "The review could not be deleted. Try again.",
                                  );
                                }
                              },
                            ),
                          ),
                        ],
                      );
                    }).toList(),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
