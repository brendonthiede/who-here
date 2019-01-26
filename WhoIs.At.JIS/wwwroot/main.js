function populateHelpText () {
  fetch('/api/slack/help')
    .then(function (response) {
      return response.text();
    })
    .then(function (text) {
      const helpTextContainer = document.getElementById('help-text');
      helpTextContainer.innerHTML = `<pre>${text.replace(/</g, '&lt;').replace(/>/g, '&gt;')}</pre>`;
    });
}

populateHelpText();