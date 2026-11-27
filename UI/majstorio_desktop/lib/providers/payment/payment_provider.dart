import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/payment/payment.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';

class PaymentProvider extends BaseProvider<Payment> {
  PaymentProvider() : super("Payments", Payment.fromJson);

  // A full PayPal refund of what was captured. The API decides the amount; only the reason is sent.
  Future<int> refund(Payment payment, String reason) async {
    var response = await http.post(
      Uri.parse("$baseUrl/Payments/${payment.id}/refund"),
      headers: authHeaders(),
      body: json.encode({"reason": reason}),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
