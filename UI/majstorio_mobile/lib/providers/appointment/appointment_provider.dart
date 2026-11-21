import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// The signed-in user's appointments (GET /Appointments/mine). Appointments are created by the API
// when an offer is accepted; the app never creates, edits or deletes them.
class AppointmentProvider extends BaseProvider<Appointment> {
  AppointmentProvider() : super("Appointments/mine", Appointment.fromJson);

  // The assigned tradesperson marks the work as done, after the start time. The API checks both.
  Future<void> complete(int id) async {
    await command("POST", "Appointments/$id/complete");
  }
}
