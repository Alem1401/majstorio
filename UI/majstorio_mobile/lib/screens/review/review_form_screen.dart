import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/providers/review/review_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// "Ocijeni": the customer rates a completed job from 1 to 5 stars, with an optional comment.
// A job can be rated only once. Pops true when the rating was saved.
class ReviewFormScreen extends StatefulWidget {
  const ReviewFormScreen({super.key, required this.appointment});

  final Appointment appointment;

  @override
  State<ReviewFormScreen> createState() => _ReviewFormScreenState();
}

class _ReviewFormScreenState extends State<ReviewFormScreen> {
  final _comment = TextEditingController();
  late ReviewProvider _reviewProvider;

  // 0 until a star is tapped.
  var rating = 0;
  var isSaving = false;
  String? ratingError;

  static const ratingNames = {1: "Loše", 2: "Slabo", 3: "Dobro", 4: "Vrlo dobro", 5: "Odlično"};

  @override
  void initState() {
    _reviewProvider = context.read<ReviewProvider>();
    super.initState();
  }

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  void save() async {
    if (rating == 0) {
      setState(() {
        ratingError = "Odaberite ocjenu od 1 do 5";
      });
      return;
    }
    var confirmed = await showConfirmDialog(
      context,
      "Poslati ocjenu $rating/5 za majstora ${widget.appointment.tradespersonName}? Ocjena se kasnije ne može mijenjati.",
    );
    if (!confirmed || !mounted) return;

    setState(() {
      isSaving = true;
    });
    try {
      await _reviewProvider.insert(widget.appointment.id, rating, _comment.text.trim());
      if (!mounted) return;
      showMessage(context, "Hvala! Ocjena je spremljena.");
      Navigator.pop(context, true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
      });
      showMessage(context, exceptionText(e));
      // 409: the job was rated meanwhile (for example on another phone). Back to the request, which reloads.
      if (e is ApiException && (e.statusCode == 409 || e.statusCode == 404)) Navigator.pop(context, true);
    }
  }

  @override
  Widget build(BuildContext context) {
    var appointment = widget.appointment;
    return MasterScreen(
      title: "Ocijeni",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                FormHeader(
                  icon: Icons.star_outline,
                  title: appointment.tradespersonName,
                  subtitle: "${appointment.serviceCategoryName} · ${formatDate(appointment.startUtc)}",
                ),
                const SizedBox(height: 24),
                const Divider(),
                const SizedBox(height: 16),
                const Text("Vaša ocjena", style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.ink)),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    for (var star = 1; star <= 5; star++)
                      IconButton(
                        iconSize: 40,
                        tooltip: "$star od 5",
                        onPressed: isSaving
                            ? null
                            : () => setState(() {
                                  rating = star;
                                  ratingError = null;
                                }),
                        icon: Icon(
                          star <= rating ? Icons.star : Icons.star_border,
                          color: AppTheme.gold,
                        ),
                      ),
                  ],
                ),
                Center(
                  child: Text(
                    rating == 0 ? "Dodirnite zvjezdicu" : ratingNames[rating]!,
                    style: const TextStyle(color: AppTheme.textMuted),
                  ),
                ),
                if (ratingError != null) ...[
                  const SizedBox(height: 4),
                  Center(child: Text(ratingError!, style: const TextStyle(color: AppTheme.danger))),
                ],
                const SizedBox(height: 24),
                TextField(
                  controller: _comment,
                  minLines: 3,
                  maxLines: 6,
                  maxLength: 2000,
                  decoration: const InputDecoration(
                    labelText: "Komentar (nije obavezno)",
                    prefixIcon: Icon(Icons.comment_outlined),
                    alignLabelWithHint: true,
                  ),
                ),
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: isSaving ? null : save,
                    icon: const Icon(Icons.send_outlined),
                    label: Text(isSaving ? "Slanje..." : "Pošalji ocjenu"),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
