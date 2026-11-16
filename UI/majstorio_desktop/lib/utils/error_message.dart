// Turns the status code of a failed API call into a sentence for the admin.
// "name" is what the call was about, for example "city" or "user".
// "conflict" is what a 409 means for this action. Only the screen knows that: on a save it means
// "already exists", on a delete it means "still in use".
String errorMessage(String name, int statusCode, {String? conflict}) {
  if (statusCode == 400) {
    return "The data entered for the $name is not valid.";
  }
  if (statusCode == 403) {
    return "You are not allowed to do this.";
  }
  if (statusCode == 404) {
    return "The $name was not found.";
  }
  if (statusCode == 409) {
    return conflict ?? "This $name already exists or is still in use.";
  }
  return "Something went wrong with the $name. Try again.";
}
