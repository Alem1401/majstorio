import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/review/review.dart';
import 'package:majstorio_desktop/models/review/review_search_request.dart';
import 'package:majstorio_desktop/providers/review/review_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/widgets/status_filter.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

class ReviewsScreen extends StatefulWidget {
  const ReviewsScreen({super.key});

  @override
  State<ReviewsScreen> createState() => _ReviewsScreenState();
}

class _ReviewsScreenState extends State<ReviewsScreen> {
  // The ratings the filter offers.
  static const ratingNames = {1: "1 star", 2: "2 stars", 3: "3 stars", 4: "4 stars", 5: "5 stars"};

  late ReviewProvider _provider;
  var reviews = <Review>[];

  // Null shows reviews with every rating.
  int? rating;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Sends the page and the rating filter to the API. The filter and the pager both call this.
  void loadReviews() async {
    var response = await _provider.get(
      ReviewSearchRequest(page: page, pageSize: pageSize, rating: rating),
    );
    if (!mounted) return;
    setState(() {
      reviews = response.items;
      totalCount = response.totalCount;
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
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            StatusFilter(
              label: "Rating",
              names: ratingNames,
              value: rating,
              onChanged: (value) {
                rating = value;
                page = 1;
                loadReviews();
              },
            ),
            const SizedBox(height: 24),
            Card(
              child: Column(
                children: [
                  LayoutBuilder(
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
                                            errorMessage("review", statusCode),
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
                  const Divider(),
                  Pager(
                    page: page,
                    pageSize: pageSize,
                    totalCount: totalCount,
                    onPageChanged: (newPage) {
                      page = newPage;
                      loadReviews();
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
