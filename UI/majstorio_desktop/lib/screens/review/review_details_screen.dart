import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/review/review.dart';
import 'package:majstorio_desktop/providers/review/review_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

class ReviewDetailsScreen extends StatefulWidget {
  const ReviewDetailsScreen({super.key, required this.toUpdate});

  // Reviews are only edited here; customers write them in the mobile app.
  final Review toUpdate;

  @override
  State<ReviewDetailsScreen> createState() => _ReviewDetailsScreenState();
}

class _ReviewDetailsScreenState extends State<ReviewDetailsScreen> {
  late ReviewProvider _provider;
  final _formKey = GlobalKey<FormState>();
  final _ratingController = TextEditingController();
  final _commentController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _provider = context.read<ReviewProvider>();
    _ratingController.text = widget.toUpdate.rating.toString();
    _commentController.text = widget.toUpdate.comment ?? "";
  }

  @override
  void dispose() {
    _ratingController.dispose();
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    var comment = _commentController.text.trim();
    var review = Review(
      id: widget.toUpdate.id,
      appointmentId: widget.toUpdate.appointmentId,
      customerName: widget.toUpdate.customerName,
      tradespersonName: widget.toUpdate.tradespersonName,
      rating: int.parse(_ratingController.text.trim()),
      comment: comment.isEmpty ? null : comment,
      createdAtUtc: widget.toUpdate.createdAtUtc,
    );
    var statusCode = await _provider.update(review);
    if (!mounted) return;
    if (statusCode == 200) {
      showMessage(context, "The review was updated.");
      Navigator.pop(context, true);
    } else {
      showMessage(context, errorMessage("review", statusCode));
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Edit review",
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppTheme.goldSoft,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.star_outline,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  "Review details",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  "${widget.toUpdate.customerName} reviewed ${widget.toUpdate.tradespersonName}.",
                                  style: const TextStyle(
                                    color: AppTheme.textMuted,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Divider(),
                      const SizedBox(height: 24),
                      TextFormField(
                        controller: _ratingController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(
                          labelText: "Rating (1 to 5)",
                          prefixIcon: Icon(Icons.star_outline),
                        ),
                        validator: (value) {
                          var rating = int.tryParse(value?.trim() ?? "");
                          if (rating == null || rating < 1 || rating > 5) {
                            return "Enter a whole number from 1 to 5";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _commentController,
                        minLines: 3,
                        maxLines: 6,
                        decoration: const InputDecoration(
                          labelText: "Comment",
                          prefixIcon: Icon(Icons.notes_outlined),
                        ),
                      ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: _save,
                          icon: const Icon(Icons.save_outlined),
                          label: const Text("Update"),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
