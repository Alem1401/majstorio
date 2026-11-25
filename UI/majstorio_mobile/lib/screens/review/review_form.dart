import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/reviews/review_upsert_request.dart';
import 'package:majstorio_mobile/providers/review/review_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

class ReviewForm extends StatefulWidget {
  const ReviewForm({super.key, required this.appointment});

  final Appointment appointment;

  @override
  State<ReviewForm> createState() => _ReviewFormState();
}

class _ReviewFormState extends State<ReviewForm> {
  late ReviewProvider provider;

  @override
  void initState() {
    provider = context.read<ReviewProvider>();
    super.initState();
  }

  @override
  void dispose() {
    commentController.dispose();
    super.dispose();
  }

  final commentController = TextEditingController();
  final key = GlobalKey<FormState>();
  // Null until the user picks a grade, so nobody sends one by accident.
  int? rating;
  // True while the rating is being sent, so it cannot be sent twice.
  var isSaving = false;

  Future<void> save() async {
    if (!key.currentState!.validate()) return;

    var confirmed = await showConfirmDialog(
      context,
      "Poslati ocjenu $rating/5? Ocjena se kasnije ne može mijenjati.",
    );
    if (!confirmed || !mounted) return;

    setState(() {
      isSaving = true;
    });
    var comment = commentController.text.trim();
    try {
      var status = await provider.post(
        ReviewUpsertRequest(
          appointmentId: widget.appointment.id,
          rating: rating!,
          comment: comment.isEmpty ? null : comment,
        ),
      );
      if (!mounted) return;
      if (status == 200) {
        showMessage(context, "Hvala! Ocjena je spremljena.");
        Navigator.pop(context, true);
        return;
      }
      showMessage(
        context,
        status == 409 ? "Ovaj posao je već ocijenjen." : "Ocjena nije spremljena. Pokušajte ponovo.",
      );
    } catch (e) {
      if (!mounted) return;
      showMessage(context, exceptionText(e));
    }
    setState(() {
      isSaving = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Ocijeni",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: key,
          child: Column(
            children: [
              Text(widget.appointment.tradespersonName),
              const SizedBox(height: 16),
              DropdownButtonFormField<int>(
                initialValue: rating,
                decoration: const InputDecoration(labelText: "Vaša ocjena"),
                items: const [
                  DropdownMenuItem(value: 1, child: Text("1 - Loše")),
                  DropdownMenuItem(value: 2, child: Text("2 - Slabo")),
                  DropdownMenuItem(value: 3, child: Text("3 - Dobro")),
                  DropdownMenuItem(value: 4, child: Text("4 - Vrlo dobro")),
                  DropdownMenuItem(value: 5, child: Text("5 - Odlično")),
                ],
                onChanged: (value) {
                  setState(() {
                    rating = value;
                  });
                },
                validator: (value) => value == null ? "Odaberite ocjenu" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: commentController,
                maxLines: 4,
                maxLength: 2000,
                decoration: const InputDecoration(labelText: "Komentar (nije obavezno)"),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: isSaving ? null : save,
                child: Text(isSaving ? "Slanje..." : "Pošalji ocjenu"),
              ),
              TextButton(
                onPressed: () {
                  Navigator.pop(context);
                },
                child: const Text("Odustani"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
