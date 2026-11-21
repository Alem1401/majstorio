import 'package:flutter/material.dart';

// Asks the user to confirm an action; true only when "Da" is tapped.
Future<bool> showConfirmDialog(BuildContext context, String message, {String title = "Potvrda"}) async {
  var result = await showDialog<bool>(
    context: context,
    builder: (context) {
      return AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context, false);
            },
            child: const Text("Ne"),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context, true);
            },
            child: const Text("Da"),
          ),
        ],
      );
    },
  );
  return result == true;
}

// Like showConfirmDialog, with an optional text field (for example the reason of a cancellation).
// Returns null when the user taps "Ne", otherwise the entered text (may be empty).
Future<String?> showReasonDialog(BuildContext context, String message, {String label = "Razlog (nije obavezno)"}) {
  return showDialog<String>(
    context: context,
    builder: (context) => _ReasonDialog(message: message, label: label),
  );
}

// The dialog owns its text field, so the field is only removed after the closing animation has finished.
class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog({required this.message, required this.label});

  final String message;
  final String label;

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text("Potvrda"),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.message),
            const SizedBox(height: 16),
            TextField(
              controller: _reason,
              maxLines: 3,
              maxLength: 2000,
              decoration: InputDecoration(labelText: widget.label),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () {
            Navigator.pop(context);
          },
          child: const Text("Ne"),
        ),
        TextButton(
          onPressed: () {
            Navigator.pop(context, _reason.text.trim());
          },
          child: const Text("Da"),
        ),
      ],
    );
  }
}

// A short message at the bottom of the screen, for the result of an action.
void showMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}
