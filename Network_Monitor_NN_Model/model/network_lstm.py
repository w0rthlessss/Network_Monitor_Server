import torch.nn as nn


class NetworkLSTM(nn.Module):

    def __init__(self, input_size, hidden_size=128, dropout=0.4):
        super(NetworkLSTM, self).__init__()

        self.lstm1 = nn.LSTM(input_size, hidden_size, batch_first=True)
        self.dropout_lstm = nn.Dropout(dropout)
        self.dropout_fc = nn.Dropout(dropout)

        self.lstm2 = nn.LSTM(hidden_size, int(hidden_size / 2), batch_first=True)

        self.fc1 = nn.Linear(int(hidden_size / 2), int(hidden_size / 4))
        self.relu = nn.ReLU()
        self.fc2 = nn.Linear(int(hidden_size / 4), 1)

    def forward(self, x):
        out, _ = self.lstm1(x)
        out = self.dropout_lstm(out)

        _, (h_n, _) = self.lstm2(out)
        out = h_n[-1]

        out = self.fc1(out)
        out = self.relu(out)
        out = self.dropout_fc(out)
        out = self.fc2(out)

        return out.squeeze(-1)
