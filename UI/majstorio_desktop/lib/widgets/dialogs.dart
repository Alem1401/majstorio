import 'package:flutter/material.dart';

// Asks the admin to confirm an action that cannot be undone; true only when "Yes" is clicked.
Future<bool> showConfirmDialog(BuildContext context, String message) async {
  var result = await showDialog<bool>(
    context: context,
    builder: (context) {
      return AlertDialog(
        title: const Text("Confirmation"),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context, true);
            },
            child: const Text("Yes"),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context, false);
            },
            child: const Text("No"),
          ),
        ],
      );
    },
  );
  return result == true;
}

// A short message at the bottom of the screen, for the result of an action.
void showMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}
